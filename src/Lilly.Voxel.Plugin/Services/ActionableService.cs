using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Rendering.Core.Primitives;
using Lilly.Voxel.Plugin.Actionables;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;
using Lilly.Voxel.Plugin.Utils;
using Serilog;

namespace Lilly.Voxel.Plugin.Services;

public class ActionableService : IActionableService
{
    private readonly ILogger _logger = Log.ForContext<ActionableService>();
    private readonly IChunkGeneratorService _chunkGenerator;
    private readonly IBlockRegistry _blockRegistry;

    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly Dictionary<ActionEventType, List<IActionableListener>> _listeners = new();
    private readonly List<IRaycastableActionableTarget> _raycastTargets = new();

    public ActionableService(
        IChunkGeneratorService chunkGenerator,
        IBlockRegistry blockRegistry,
        IMainThreadDispatcher mainThreadDispatcher
    )
    {
        _chunkGenerator = chunkGenerator;
        _blockRegistry = blockRegistry;
        _mainThreadDispatcher = mainThreadDispatcher;
    }

    public void AddActionListener(IActionableListener listener)
    {
        if (!_listeners.TryGetValue(listener.EventType, out var value))
        {
            value = new();
            _listeners[listener.EventType] = value;
        }

        value.Add(listener);
    }

    public void Handle(ActionEventContext ctx)
    {
        var target = ctx.Target;

        if (target is null && !TryGetInstance(ctx.WorldPosition, out target))
        {
            return;
        }

        Dispatch(ctx with { Target = target });
    }

    public void OnPlace(Vector3 worldPos, ushort blockTypeId, BlockType type)
    {
        if (blockTypeId == _blockRegistry.Air.Id)
        {
            return;
        }

        if (!TryResolve(worldPos, out var chunk, out var localIndex))
        {
            _logger.Debug("OnPlace: chunk not loaded at {Position}", worldPos);

            return;
        }

        chunk.EnsureActionable(localIndex, blockTypeId);
        _ = type;
    }

    public void OnRemove(Vector3 worldPos)
    {
        if (!TryResolve(worldPos, out var chunk, out var localIndex))
        {
            return;
        }

        chunk.RemoveActionable(localIndex);
    }

    public bool TryGetInstance(Vector3 worldPos, out IActionableTarget inst)
    {
        inst = null!;

        return TryResolve(worldPos, out var chunk, out var localIndex) && chunk.TryGetActionable(localIndex, out inst);
    }

    public void RegisterRaycastTarget(IRaycastableActionableTarget target)
    {
        if (!_raycastTargets.Contains(target))
        {
            _raycastTargets.Add(target);
        }
    }

    public void UnregisterRaycastTarget(IRaycastableActionableTarget target)
        => _raycastTargets.Remove(target);

    public bool TryRaycastTarget(Ray ray, float maxDistance, out IActionableTarget target, out Vector3 hitPoint)
    {
        target = null!;
        hitPoint = default;
        var bestDistance = float.MaxValue;

        for (var i = _raycastTargets.Count - 1; i >= 0; i--)
        {
            var t = _raycastTargets[i];

            if (t == null)
            {
                _raycastTargets.RemoveAt(i);
                continue;
            }

            if (!t.Raycast(ray, maxDistance, out var distance, out var hp))
            {
                continue;
            }

            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            target = t;
            hitPoint = hp;
        }

        return bestDistance < float.MaxValue;
    }

    public void Update(GameTime gameTime)
    {
        foreach (var chunk in _chunkGenerator.GetActiveChunks())
        {
            if (chunk.Actionables is null)
            {
                continue;
            }

            foreach (var instance in chunk.Actionables.Values)
            {
                var worldPos = ChunkEntity.GetWorldPosition(chunk, instance.LocalIndex);
                var ctx = new ActionEventContext
                {
                    Event = ActionEventType.OnTick,
                    WorldPosition = worldPos,
                    GameTime = gameTime,
                    Target = instance
                };

                Dispatch(ctx);
            }
        }
    }

    private void Dispatch(ActionEventContext ctx)
    {
        if (ctx.Target is null)
        {
            return;
        }

        if (!_listeners.TryGetValue(ctx.Event, out var listeners))
        {
            return;
        }

        foreach (var listener in listeners)
        {
            if (!listener.CanHandle(ctx.Target))
            {
                continue;
            }

            _logger.Debug(
                "Dispatching {Event} to listener {Listener} via MainThreadDispatcher",
                ctx.Event,
                listener.GetType().Name
            );

            _mainThreadDispatcher.EnqueueAction(() => listener.DispatchAction(ctx));
        }

        // // Simple sound handling on use/place
        // if (ctx.Event is ActionEventType.OnUse or ActionEventType.OnPlace && ctx.Target is not null)
        // {
        //     var sound = ctx.Target.Components.Get<SoundComponent>();
        //
        //     if (sound != null)
        //     {
        //         _audioService.PlaySoundEffect3D(sound.soundId, ctx.WorldPosition);
        //     }
        // }
    }

    private bool TryResolve(Vector3 worldPos, out ChunkEntity chunk, out int localIndex)
    {
        localIndex = -1;
        chunk = null!;

        var chunkCoords = ChunkUtils.GetChunkCoordinates(worldPos);
        var chunkWorld = ChunkUtils.ChunkCoordinatesToWorldPosition(
            (int)chunkCoords.X,
            (int)chunkCoords.Y,
            (int)chunkCoords.Z
        );

        if (!_chunkGenerator.TryGetCachedChunk(chunkWorld, out var chunkEntity) || chunkEntity is null)
        {
            return false;
        }

        var (lx, ly, lz) = ChunkUtils.GetLocalIndices(worldPos);

        if (!ChunkUtils.IsValidLocalPosition(lx, ly, lz))
        {
            return false;
        }

        localIndex = ChunkEntity.GetIndex(lx, ly, lz);
        chunk = chunkEntity;

        return true;
    }
}
