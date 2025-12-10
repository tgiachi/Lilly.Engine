using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Data.Physics;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using Lilly.Voxel.Plugin.Actionables;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Data;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Services;
using Lilly.Voxel.Plugin.Types;
using Lilly.Voxel.Plugin.Utils;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Manages chunk streaming and rendering around the player using the job system.
/// </summary>
public sealed class WorldGameObject : Base3dGameObject
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly IAssetManager _assetManager;
    private readonly ChunkMeshBuilder _meshBuilder;
    private readonly ChunkColliderBuilder _colliderBuilder;
    private readonly IJobSystemService _jobSystem;
    private readonly IChunkGeneratorService _chunkGenerator;
    private readonly IGameObjectManager _gameObjectManager;
    private readonly ICamera3dService _cameraService;
    private readonly ChunkLightPropagationService _lightPropagationService;
    private readonly IPhysicWorld3d _physicsWorld;

    private readonly IActionableService _actionableService;

    private readonly IBlockRegistry _blockRegistry;

    private readonly ConcurrentDictionary<Vector3, ChunkGameObject> _activeChunks = new();
    private readonly ConcurrentDictionary<Vector3, IJobHandle<ChunkBuildResult>> _pending = new();
    private readonly ConcurrentDictionary<Vector3, ChunkColliderData> _colliderCache = new();
    private readonly ConcurrentDictionary<Vector3, List<IPhysicsBodyHandle>> _physicsBodies = new();
    private readonly HashSet<Vector3> _physicsDirty = new();
    private readonly List<Vector3> _targetOffsets = new();
    private readonly HashSet<Vector3> _targetScratch = new();
    private int _cachedHorizontalRadius = int.MinValue;
    private int _cachedVerticalBelow = int.MinValue;
    private int _cachedVerticalAbove = int.MinValue;
    private bool _isWorldReady;

    public ChunkStreamingConfiguration StreamingConfig { get; } = new();

    public bool IsChunkDebuggerVisible
    {
        get => GetGameObject<ChunkDebuggerViewerGameObject>().IsActive;
        set => GetGameObject<ChunkDebuggerViewerGameObject>().IsActive = value;
    }

    public bool IsBlockOutlineVisible
    {
        get => GetGameObject<BlockOutlineGameObject>().IsActive;
        set => GetGameObject<BlockOutlineGameObject>().IsActive = value;
    }

    public WorldGameObject(
        GraphicsDevice graphicsDevice,
        IAssetManager assetManager,
        ChunkMeshBuilder meshBuilder,
        ChunkColliderBuilder colliderBuilder,
        IJobSystemService jobSystem,
        IChunkGeneratorService chunkGenerator,
        IGameObjectManager gameObjectManager,
        ICamera3dService cameraService,
        IBlockRegistry blockRegistry,
        ChunkLightPropagationService lightPropagationService,
        IActionableService actionableService,
        IPhysicWorld3d physicsWorld
    ) : base("World", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _meshBuilder = meshBuilder;
        _colliderBuilder = colliderBuilder;
        _jobSystem = jobSystem;
        _chunkGenerator = chunkGenerator;
        _gameObjectManager = gameObjectManager;
        _cameraService = cameraService;
        _blockRegistry = blockRegistry;
        _lightPropagationService = lightPropagationService;
        _actionableService = actionableService;
        _physicsWorld = physicsWorld;
        IgnoreFrustumCulling = true;
    }

    /// <summary>
    /// Raised when all target chunks for the current streaming window are generated and active.
    /// Fires again after the world becomes not-ready (e.g., player moves) and returns to ready.
    /// </summary>
    public event Action? WorldGenerated;

    public bool GetBlockAtPosition(Vector3 position, out BlockType blockType)
    {
        var chunkCoords = ChunkUtils.GetChunkCoordinates(position);
        blockType = _blockRegistry.Air;

        if (!_activeChunks.TryGetValue(chunkCoords, out var chunkGo))
        {
            return false;
        }

        var (lx, ly, lz) = ChunkUtils.GetLocalIndices(position);

        if (!ChunkUtils.IsValidLocalPosition(lx, ly, lz))
        {
            return false;
        }

        var blockId = chunkGo.Chunk.GetBlock(lx, ly, lz);
        blockType = _blockRegistry.GetById(blockId);

        return blockId != 0;
    }

    public override void Initialize()
    {
        CreateGameObject<CrosshairGameObject>();
        CreateGameObject<BlockOutlineGameObject>();
        CreateGameObject<ChunkDebuggerViewerGameObject>();

        IsBlockOutlineVisible = true;
        IsChunkDebuggerVisible = true;

        base.Initialize();
    }

    public bool Raycast(Ray ray, float maxDistance, out Vector3 blockPosition)
    {
        // Normalize direction to keep step size consistent regardless of input
        var direction = Vector3.Normalize(ray.Direction);
        var origin = ray.Origin;
        const float step = 0.1f;
        var distance = 0f;

        while (distance <= maxDistance)
        {
            var sample = origin + direction * distance;
            var samplePos = new Vector3(sample.X, sample.Y, sample.Z);
            var chunkCoords = ChunkUtils.GetChunkCoordinates(samplePos);

            if (_activeChunks.TryGetValue(chunkCoords, out var chunkGo))
            {
                var (localX, localY, localZ) = ChunkUtils.GetLocalIndices(samplePos);

                if (ChunkUtils.IsValidLocalPosition(localX, localY, localZ))
                {
                    var blockId = chunkGo.Chunk.GetBlock(localX, localY, localZ);

                    if (blockId != 0)
                    {
                        blockPosition = new(
                            (int)MathF.Floor(samplePos.X),
                            (int)MathF.Floor(samplePos.Y),
                            (int)MathF.Floor(samplePos.Z)
                        );

                        return true;
                    }
                }
            }

            distance += step;
        }

        blockPosition = default;

        return false;
    }

    public bool RemoveBlockAtCurrentPosition()
    {
        var blockOutline = GetGameObject<BlockOutlineGameObject>();

        if (blockOutline.TargetBlockPosition.HasValue)
        {
            RemoveBlockAtPosition(blockOutline.TargetBlockPosition.Value);

            return true;
        }

        return false;
    }

    public void RemoveBlockAtPosition(Vector3 position)
    {
        SetBlockAtPosition(position, _blockRegistry.Air);
    }

    public void SetBlockAtPosition(Vector3 position, BlockType blockType)
    {
        var chunkCoords = ChunkUtils.GetChunkCoordinates(position);

        if (!_activeChunks.TryGetValue(chunkCoords, out var chunkGo))
        {
            return;
        }

        var (lx, ly, lz) = ChunkUtils.GetLocalIndices(position);

        if (!ChunkUtils.IsValidLocalPosition(lx, ly, lz))
        {
            return;
        }

        var chunk = chunkGo.Chunk;
        chunk.SetBlock(lx, ly, lz, blockType.Id);
        chunk.IsLightingDirty = true;
        chunk.IsMeshDirty = true;
        chunk.IsModified = true;
        _physicsDirty.Add(chunkCoords);

        EnqueueChunkRebuild(chunkCoords, chunkGo);
        UpdateNeighbors(chunkCoords);

        if (blockType == _blockRegistry.Air)
        {
            _actionableService.OnRemove(position);
            _actionableService.Handle(
                new()
                {
                    Event = ActionEventType.OnBreak,
                    WorldPosition = position,
                    Target = null
                }
            );
        }
        else
        {
            _actionableService.OnPlace(position, blockType.Id, blockType);

            _actionableService.Handle(
                new()
                {
                    Event = ActionEventType.OnPlace,
                    WorldPosition = position,
                    Target = null
                }
            );
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var playerChunk = ChunkUtils.GetChunkCoordinates(GetStreamingOrigin());
        var targets = BuildTargetSet(playerChunk);
        var physicsTargets = BuildPhysicsTargetSet(playerChunk);

        ScheduleMissingChunks(targets);
        ProcessCompletedJobs();
        UnloadFarChunks(targets);
        UpdatePhysicsAttachments(physicsTargets);
        NotifyWorldReadyIfNeeded(targets);

        _actionableService.Update(gameTime);
    }

    public void UseBlockAtCurrentPosition()
    {
        const float maxUseDistance = 8f;
        var camera = _cameraService.ActiveCamera;
        var bestDistance = float.MaxValue;
        IActionableTarget? chosenTarget = null;
        var chosenPosition = Vector3.Zero;
        var hasHit = false;

        if (camera != null)
        {
            var ray = new Ray(camera.Position, Vector3.Normalize(camera.Forward));

            if (_actionableService.TryRaycastTarget(ray, maxUseDistance, out var target, out var hitPoint))
            {
                var dist = (hitPoint - camera.Position).Length();
                chosenTarget = target;
                chosenPosition = hitPoint;
                bestDistance = dist;
                hasHit = true;
            }
        }

        var blockOutline = GetGameObject<BlockOutlineGameObject>();

        if (blockOutline.TargetBlockPosition.HasValue)
        {
            var pos = blockOutline.TargetBlockPosition.Value;

            if (GetBlockAtPosition(pos, out var blockType))
            {
                var blockCenter = pos + new Vector3(0.5f, 0.5f, 0.5f);
                var dist = camera != null ? (blockCenter - camera.Position).Length() : 0f;

                if (dist < bestDistance && dist <= maxUseDistance)
                {
                    chosenTarget = null;
                    chosenPosition = pos;
                    bestDistance = dist;
                    hasHit = true;
                }
            }
        }

        if (hasHit)
        {
            _actionableService.Handle(
                new ActionEventContext
                {
                    Event = ActionEventType.OnUse,
                    WorldPosition = chosenPosition,
                    Target = chosenTarget
                }
            );
        }
    }

    private void AttachPhysics(Vector3 coord, ChunkGameObject chunkGo, ChunkColliderData colliderData)
    {
        if (colliderData.IsEmpty)
        {
            return;
        }

        var handles = new List<IPhysicsBodyHandle>(colliderData.Boxes.Count);

        foreach (var box in colliderData.Boxes)
        {
            var size = box.Max - box.Min;
            var center = box.Min + size * 0.5f + chunkGo.Chunk.Position;

            var cfg = new PhysicsBodyConfig(
                new BoxShape(size.X, size.Y, size.Z),
                0f,
                new(center, Quaternion.Identity)
            );

            var handle = _physicsWorld.CreateStatic(cfg.Shape, cfg.Pose);
            handles.Add(handle);
        }

        if (handles.Count > 0)
        {
            _physicsBodies[coord] = handles;
        }
    }

    private HashSet<Vector3> BuildPhysicsTargetSet(Vector3 playerChunk)
    {
        var targets = new HashSet<Vector3>();

        for (var dx = -StreamingConfig.PhysicsRadiusChunks; dx <= StreamingConfig.PhysicsRadiusChunks; dx++)
        {
            for (var dz = -StreamingConfig.PhysicsRadiusChunks; dz <= StreamingConfig.PhysicsRadiusChunks; dz++)
            {
                for (var dy = -StreamingConfig.PhysicsVerticalBelowChunks;
                     dy <= StreamingConfig.PhysicsVerticalAboveChunks;
                     dy++)
                {
                    targets.Add(playerChunk + new Vector3(dx, dy, dz));
                }
            }
        }

        return targets;
    }

    private HashSet<Vector3> BuildTargetSet(Vector3 playerChunk)
    {
        RebuildTargetOffsetsIfNeeded();
        _targetScratch.Clear();

        foreach (var offset in _targetOffsets)
        {
            _targetScratch.Add(playerChunk + offset);
        }

        return _targetScratch;
    }

    private void EnqueueChunkRebuild(Vector3 coord, ChunkGameObject chunkGo)
    {
        if (_pending.ContainsKey(coord))
        {
            return;
        }

        var job = _jobSystem.Schedule(
            $"chunk_rebuild_{coord.ToHumanReadableString()}",
            async ct =>
            {
                var chunk = chunkGo.Chunk;

                ChunkEntity? topNeighbor = null;
                var topPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                    (int)coord.X,
                    (int)coord.Y + 1,
                    (int)coord.Z
                );

                if (_chunkGenerator.TryGetCachedChunk(topPos, out var t))
                {
                    topNeighbor = t;
                }

                _lightPropagationService.PropagateLight(chunk, topNeighbor);

                var mesh = _meshBuilder.BuildMeshData(
                    chunk,
                    chunkCoords =>
                    {
                        if (_activeChunks.TryGetValue(chunkCoords, out var nGo))
                        {
                            return nGo.Chunk;
                        }

                        var nPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                            (int)chunkCoords.X,
                            (int)chunkCoords.Y,
                            (int)chunkCoords.Z
                        );

                        return _chunkGenerator.TryGetCachedChunk(nPos, out var n) ? n : null;
                    }
                );

                var collider = _colliderBuilder.Build(chunk);

                return new ChunkBuildResult(chunk, mesh, collider);
            }
        );

        _pending[coord] = job;
    }

    private Vector3 GetStreamingOrigin()
        => _cameraService.ActiveCamera?.Position ?? Transform.Position;

    private void NotifyWorldReadyIfNeeded(HashSet<Vector3> targets)
    {
        var allTargetsActive = targets.All(coord => _activeChunks.ContainsKey(coord));
        var isReady = allTargetsActive && _pending.IsEmpty;

        if (isReady && !_isWorldReady)
        {
            _isWorldReady = true;
            WorldGenerated?.Invoke();
        }
        else if (!isReady && _isWorldReady)
        {
            _isWorldReady = false;
        }
    }

    private void ProcessCompletedJobs()
    {
        foreach (var kvp in _pending.ToArray())
        {
            var coord = kvp.Key;
            var handle = kvp.Value;

            if (!handle.IsCompleted)
            {
                continue;
            }

            var result = handle.CompletionTask.GetAwaiter().GetResult();
            _pending.TryRemove(coord, out _);

            var isNewChunk = false;

            if (_activeChunks.TryGetValue(coord, out var existing))
            {
                existing.SetPendingMesh(result.MeshData);
                existing.ColliderData = result.ColliderData;
            }
            else
            {
                var chunkGo = new ChunkGameObject(result.Chunk, _graphicsDevice, _assetManager, _gameObjectManager);
                chunkGo.SetPendingMesh(result.MeshData);
                chunkGo.ColliderData = result.ColliderData;
                _activeChunks[coord] = chunkGo;
                isNewChunk = true;

                // Add to scene graph so it gets rendered and updated automatically
                AddGameObject(chunkGo);
            }

            _colliderCache[coord] = result.ColliderData;
            _physicsDirty.Add(coord);

            // If a new chunk was added, notify neighbors to rebuild their mesh
            // (so they can hide faces that are now touching this new chunk)
            if (isNewChunk)
            {
                UpdateNeighbors(coord);
            }
        }
    }

    private void RebuildTargetOffsetsIfNeeded()
    {
        if (_cachedHorizontalRadius == StreamingConfig.HorizontalRadiusChunks &&
            _cachedVerticalBelow == StreamingConfig.VerticalBelowChunks &&
            _cachedVerticalAbove == StreamingConfig.VerticalAboveChunks)
        {
            return;
        }

        _cachedHorizontalRadius = StreamingConfig.HorizontalRadiusChunks;
        _cachedVerticalBelow = StreamingConfig.VerticalBelowChunks;
        _cachedVerticalAbove = StreamingConfig.VerticalAboveChunks;
        _targetOffsets.Clear();

        for (var dx = -StreamingConfig.HorizontalRadiusChunks; dx <= StreamingConfig.HorizontalRadiusChunks; dx++)
        {
            for (var dz = -StreamingConfig.HorizontalRadiusChunks; dz <= StreamingConfig.HorizontalRadiusChunks; dz++)
            {
                for (var dy = -StreamingConfig.VerticalBelowChunks; dy <= StreamingConfig.VerticalAboveChunks; dy++)
                {
                    _targetOffsets.Add(new(dx, dy, dz));
                }
            }
        }
    }

    private void RemovePhysics(Vector3 coord)
    {
        if (_physicsBodies.TryRemove(coord, out var handles))
        {
            foreach (var handle in handles)
            {
                _physicsWorld.Remove(handle);
            }
        }
        _physicsDirty.Remove(coord);
    }

    private void ScheduleMissingChunks(HashSet<Vector3> targets)
    {
        foreach (var coord in targets)
        {
            if (_activeChunks.ContainsKey(coord) || _pending.ContainsKey(coord))
            {
                continue;
            }

            if (_pending.Count >= StreamingConfig.MaxConcurrentJobs)
            {
                break;
            }

            var job = _jobSystem.Schedule(
                $"chunk_build_{coord.ToHumanReadableString()}",
                async ct =>
                {
                    var worldPos = ChunkUtils.ChunkCoordinatesToWorldPosition((int)coord.X, (int)coord.Y, (int)coord.Z);
                    var chunk = await _chunkGenerator.GetChunkByWorldPosition(worldPos);

                    // Pass neighbor lookup to MeshBuilder so it can cull faces between chunks
                    var mesh = _meshBuilder.BuildMeshData(
                        chunk,
                        chunkCoords =>
                        {
                            if (_activeChunks.TryGetValue(chunkCoords, out var neighborGo))
                            {
                                return neighborGo.Chunk;
                            }

                            var neighborPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                                (int)chunkCoords.X,
                                (int)chunkCoords.Y,
                                (int)chunkCoords.Z
                            );

                            if (_chunkGenerator.TryGetCachedChunk(neighborPos, out var neighbor))
                            {
                                return neighbor;
                            }

                            return null;
                        }
                    );

                    var collider = _colliderBuilder.Build(chunk);

                    return new ChunkBuildResult(chunk, mesh, collider);
                }
            );

            _pending[coord] = job;
        }
    }

    private void UnloadFarChunks(HashSet<Vector3> targets)
    {
        var toRemove = new List<Vector3>();

        foreach (var coord in _activeChunks.Keys)
        {
            if (!targets.Contains(coord))
            {
                toRemove.Add(coord);
            }
        }

        foreach (var coord in toRemove)
        {
            if (_activeChunks.Remove(coord, out var chunk))
            {
                chunk.DisposeBuffers();
                RemovePhysics(coord);
                _colliderCache.TryRemove(coord, out _);

                // Remove from scene graph
                RemoveGameObject(chunk);
            }
        }
    }

    private void UpdateNeighbors(Vector3 coord)
    {
        var directions = new[]
        {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0), new Vector3(0, -1, 0),
            new Vector3(0, 0, 1), new Vector3(0, 0, -1)
        };

        foreach (var dir in directions)
        {
            var neighborCoord = coord + dir;

            if (_activeChunks.TryGetValue(neighborCoord, out var neighborGo))
            {
                if (!_pending.ContainsKey(neighborCoord))
                {
                    var job = _jobSystem.Schedule(
                        $"chunk_rebuild_{neighborCoord.ToHumanReadableString()}",
                        async ct =>
                        {
                            var chunk = neighborGo.Chunk; // We already have the chunk, no need to await GetChunk

                            ChunkEntity? topNeighbor = null;
                            var topPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                                (int)neighborCoord.X,
                                (int)neighborCoord.Y + 1,
                                (int)neighborCoord.Z
                            );

                            if (_chunkGenerator.TryGetCachedChunk(topPos, out var t))
                            {
                                topNeighbor = t;
                            }

                            _lightPropagationService.PropagateLight(chunk, topNeighbor);

                            var mesh = _meshBuilder.BuildMeshData(
                                chunk,
                                chunkCoords =>
                                {
                                    if (_activeChunks.TryGetValue(chunkCoords, out var nGo))
                                    {
                                        return nGo.Chunk;
                                    }

                                    var nPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                                        (int)chunkCoords.X,
                                        (int)chunkCoords.Y,
                                        (int)chunkCoords.Z
                                    );

                                    if (_chunkGenerator.TryGetCachedChunk(nPos, out var n))
                                    {
                                        return n;
                                    }

                                    return null;
                                }
                            );

                            var collider = _colliderBuilder.Build(chunk);

                            return new ChunkBuildResult(chunk, mesh, collider);
                        }
                    );

                    _pending[neighborCoord] = job;
                }
            }
        }
    }

    private void UpdatePhysicsAttachments(HashSet<Vector3> physicsTargets)
    {
        var attachBudget = Math.Max(1, StreamingConfig.PhysicsAttachPerFrame);
        var attachedThisFrame = 0;

        foreach (var target in physicsTargets)
        {
            if (!_activeChunks.TryGetValue(target, out var chunkGo))
            {
                continue;
            }

            if (_pending.ContainsKey(target) && _physicsDirty.Contains(target))
            {
                RemovePhysics(target);

                continue;
            }

            if (!_colliderCache.TryGetValue(target, out var colliders) || colliders.IsEmpty)
            {
                continue;
            }

            var needsAttach = !_physicsBodies.ContainsKey(target) || _physicsDirty.Contains(target);

            if (!needsAttach || attachedThisFrame >= attachBudget)
            {
                continue;
            }

            RemovePhysics(target);
            AttachPhysics(target, chunkGo, colliders);
            _physicsDirty.Remove(target);
            attachedThisFrame++;
        }

        var toDetach = new List<Vector3>();

        foreach (var kvp in _physicsBodies)
        {
            if (!physicsTargets.Contains(kvp.Key))
            {
                toDetach.Add(kvp.Key);
            }
        }

        foreach (var coord in toDetach)
        {
            RemovePhysics(coord);
        }
    }
}
