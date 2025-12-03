using System.Collections.Concurrent;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Primitives;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Services;
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
    private readonly IJobSystemService _jobSystem;
    private readonly IChunkGeneratorService _chunkGenerator;
    private readonly IGameObjectManager _gameObjectManager;
    private readonly ICamera3dService _cameraService;

    private readonly IBlockRegistry _blockRegistry;

    private readonly Dictionary<Vector3, ChunkGameObject> _activeChunks = new();
    private readonly ConcurrentDictionary<Vector3, IJobHandle<ChunkBuildResult>> _pending = new();
    private readonly List<Vector3> _targetOffsets = new();
    private readonly HashSet<Vector3> _targetScratch = new();
    private int _cachedHorizontalRadius = int.MinValue;
    private int _cachedVerticalBelow = int.MinValue;
    private int _cachedVerticalAbove = int.MinValue;

    public int HorizontalRadiusChunks { get; set; } = 4;
    public int VerticalBelowChunks { get; set; } = 1;
    public int VerticalAboveChunks { get; set; } = 4;
    public int MaxConcurrentJobs { get; set; } = 4;

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
        IJobSystemService jobSystem,
        IChunkGeneratorService chunkGenerator,
        IGameObjectManager gameObjectManager,
        ICamera3dService cameraService,
        IBlockRegistry blockRegistry
    ) : base("World", gameObjectManager)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _meshBuilder = meshBuilder;
        _jobSystem = jobSystem;
        _chunkGenerator = chunkGenerator;
        _gameObjectManager = gameObjectManager;
        _cameraService = cameraService;
        _blockRegistry = blockRegistry;
        IgnoreFrustumCulling = true;
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

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var playerChunk = ChunkUtils.GetChunkCoordinates(GetStreamingOrigin());
        var targets = BuildTargetSet(playerChunk);

        ScheduleMissingChunks(targets);
        ProcessCompletedJobs();
        UnloadFarChunks(targets);
    }

    private Vector3 GetStreamingOrigin()
    {
        return _cameraService.ActiveCamera?.Position ?? Transform.Position;
    }

    private void RebuildTargetOffsetsIfNeeded()
    {
        if (_cachedHorizontalRadius == HorizontalRadiusChunks &&
            _cachedVerticalBelow == VerticalBelowChunks &&
            _cachedVerticalAbove == VerticalAboveChunks)
        {
            return;
        }

        _cachedHorizontalRadius = HorizontalRadiusChunks;
        _cachedVerticalBelow = VerticalBelowChunks;
        _cachedVerticalAbove = VerticalAboveChunks;
        _targetOffsets.Clear();

        for (int dx = -HorizontalRadiusChunks; dx <= HorizontalRadiusChunks; dx++)
        {
            for (int dz = -HorizontalRadiusChunks; dz <= HorizontalRadiusChunks; dz++)
            {
                for (int dy = -VerticalBelowChunks; dy <= VerticalAboveChunks; dy++)
                {
                    _targetOffsets.Add(new Vector3(dx, dy, dz));
                }
            }
        }
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

    private void ScheduleMissingChunks(HashSet<Vector3> targets)
    {
        foreach (var coord in targets)
        {
            if (_activeChunks.ContainsKey(coord) || _pending.ContainsKey(coord))
            {
                continue;
            }

            if (_pending.Count >= MaxConcurrentJobs)
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
                        (chunkCoords) =>
                        {
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

                    return new ChunkBuildResult(chunk, mesh);
                }
            );

            _pending[coord] = job;
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

            bool isNewChunk = false;

            if (_activeChunks.TryGetValue(coord, out var existing))
            {
                existing.SetPendingMesh(result.MeshData);
            }
            else
            {
                var chunkGo = new ChunkGameObject(result.Chunk, _graphicsDevice, _assetManager, _gameObjectManager);
                chunkGo.SetPendingMesh(result.MeshData);
                _activeChunks[coord] = chunkGo;
                isNewChunk = true;

                // Add to scene graph so it gets rendered and updated automatically
                AddGameObject(chunkGo);
            }

            // If a new chunk was added, notify neighbors to rebuild their mesh
            // (so they can hide faces that are now touching this new chunk)
            if (isNewChunk)
            {
                UpdateNeighbors(coord);
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

                            var mesh = _meshBuilder.BuildMeshData(
                                chunk,
                                (chunkCoords) =>
                                {
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

                            return new ChunkBuildResult(chunk, mesh);
                        }
                    );

                    _pending[neighborCoord] = job;
                }
            }
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

                // Remove from scene graph
                RemoveGameObject(chunk);
            }
        }
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
                        blockPosition = new Vector3(
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

        EnqueueChunkRebuild(chunkCoords, chunkGo);
        UpdateNeighbors(chunkCoords);
    }

    public void RemoveBlockAtPosition(Vector3 position)
    {
        SetBlockAtPosition(position, _blockRegistry.Air);
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
                var mesh = _meshBuilder.BuildMeshData(
                    chunk,
                    chunkCoords =>
                    {
                        var nPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                            (int)chunkCoords.X,
                            (int)chunkCoords.Y,
                            (int)chunkCoords.Z
                        );

                        return _chunkGenerator.TryGetCachedChunk(nPos, out var n) ? n : null;
                    }
                );

                return new ChunkBuildResult(chunk, mesh);
            }
        );

        _pending[coord] = job;
    }
}
