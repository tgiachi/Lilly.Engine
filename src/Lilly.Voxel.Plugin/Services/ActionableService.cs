using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Voxel.Plugin.Actionables;
using Lilly.Voxel.Plugin.Actionables.Components;
using Lilly.Voxel.Plugin.Blocks;
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
    private readonly IAudioService _audioService;
    private readonly IBlockRegistry _blockRegistry;

    public ActionableService(
        IChunkGeneratorService chunkGenerator,
        IAudioService audioService,
        IBlockRegistry blockRegistry
    )
    {
        _chunkGenerator = chunkGenerator;
        _audioService = audioService;
        _blockRegistry = blockRegistry;
    }

    public bool TryGetInstance(Vector3 worldPos, out BlockInstance inst)
    {
        inst = null!;

        return TryResolve(worldPos, out var chunk, out var localIndex) && chunk.TryGetActionable(localIndex, out inst);
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
        _ = type; // preserve hook for future component defaults
    }

    public void OnRemove(Vector3 worldPos)
    {
        if (!TryResolve(worldPos, out var chunk, out var localIndex))
        {
            return;
        }

        chunk.RemoveActionable(localIndex);
    }

    public void Handle(ActionEventContext ctx)
    {
        var instance = ctx.Instance;

        if (instance is null && !TryGetInstance(ctx.WorldPosition, out instance))
        {
            return;
        }

        Dispatch(ctx with { Instance = instance });
    }

    public void TickActive(GameTime gameTime)
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
                var ctx = new ActionEventContext(
                    ActionEventType.OnTick,
                    worldPos,
                    gameTime,
                    instance
                );

                Dispatch(ctx);
            }
        }
    }

    private void Dispatch(ActionEventContext ctx)
    {
        if (ctx.Instance is null)
        {
            return;
        }

        // Simple sound handling on use/place
        if (ctx.Event is ActionEventType.OnUse or ActionEventType.OnPlace)
        {
            var sound = ctx.Instance.Components.Get<SoundComponent>();

            if (sound != null)
            {
                _audioService.PlaySoundEffect3D(sound.soundId, ctx.WorldPosition);
            }
        }
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
