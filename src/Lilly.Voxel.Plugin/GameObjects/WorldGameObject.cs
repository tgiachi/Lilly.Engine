using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Services;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Utils;
using System.Linq;
using System.Collections.Concurrent;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Core.Interfaces.Services;
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

    private readonly Dictionary<Vector3, ChunkGameObject> _activeChunks = new();
    private readonly ConcurrentDictionary<Vector3, IJobHandle<ChunkBuildResult>> _pending = new();

    public int HorizontalRadiusChunks { get; set; } = 4;
    public int VerticalBelowChunks { get; set; } = 1;
    public int VerticalAboveChunks { get; set; } = 0;
    public int MaxConcurrentJobs { get; set; } = 4;

    public WorldGameObject(
        GraphicsDevice graphicsDevice,
        IAssetManager assetManager,
        ChunkMeshBuilder meshBuilder,
        IJobSystemService jobSystem,
        IChunkGeneratorService chunkGenerator
    ) : base("World", null!)
    {
        _graphicsDevice = graphicsDevice;
        _assetManager = assetManager;
        _meshBuilder = meshBuilder;
        _jobSystem = jobSystem;
        _chunkGenerator = chunkGenerator;
        IgnoreFrustumCulling = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var playerChunk = ChunkUtils.GetChunkCoordinates(Transform.Position);
        var targets = BuildTargetSet(playerChunk);

        ScheduleMissingChunks(targets);
        ProcessCompletedJobs();
        UnloadFarChunks(targets);

        foreach (var chunk in _activeChunks.Values)
        {
            chunk.Update(gameTime);
        }
    }

    public override void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera)
    {
        foreach (var chunk in _activeChunks.Values)
        {
            chunk.Draw(gameTime, graphicsDevice, camera);
        }
    }

    private HashSet<Vector3> BuildTargetSet(Vector3 playerChunk)
    {
        var targets = new HashSet<Vector3>();

        for (int dx = -HorizontalRadiusChunks; dx <= HorizontalRadiusChunks; dx++)
        {
            for (int dz = -HorizontalRadiusChunks; dz <= HorizontalRadiusChunks; dz++)
            {
                for (int dy = -VerticalBelowChunks; dy <= VerticalAboveChunks; dy++)
                {
                    targets.Add(new Vector3(playerChunk.X + dx, playerChunk.Y + dy, playerChunk.Z + dz));
                }
            }
        }

        return targets;
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
                $"chunk_build_{coord}",
                async ct =>
                {
                    var worldPos = ChunkUtils.ChunkCoordinatesToWorldPosition((int)coord.X, (int)coord.Y, (int)coord.Z);
                    var chunk = await _chunkGenerator.GetChunkByWorldPosition(worldPos);
                    
                    // Pass neighbor lookup to MeshBuilder so it can cull faces between chunks
                    var mesh = _meshBuilder.BuildMeshData(chunk, (chunkCoords) => 
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
                    });
                    
                    return new ChunkBuildResult(chunk, mesh);
                },
                JobPriority.Normal,
                onComplete: null,
                cancellationToken: default
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
                var chunkGo = new ChunkGameObject(result.Chunk, _graphicsDevice, _assetManager, null!);
                chunkGo.SetPendingMesh(result.MeshData);
                _activeChunks[coord] = chunkGo;
                isNewChunk = true;
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
                // Mark the neighbor as dirty so it rebuilds its mesh in the next Update loop
                // (or whenever the system handles dirty chunks)
                // Since we don't have a direct "Rebuild" method exposed here that schedules a job,
                // we rely on the ChunkEntity's dirty flag if the system checks it, 
                // OR we manually trigger a rebuild schedule.
                //
                // Currently, WorldGameObject schedules based on _activeChunks check. 
                // We need to force it to re-schedule or re-build.
                
                // For now, we'll remove it from active chunks so it gets picked up as "missing" 
                // and re-scheduled. This is a bit heavy but safe.
                // A better way would be to have a "Dirty" set or explicit Rebuild method.
                
                // Actually, let's just schedule a job for it directly if not already pending.
                if (!_pending.ContainsKey(neighborCoord))
                {
                    // Force a re-schedule by removing from active? 
                    // No, that destroys the object.
                    // Let's just launch a new job for it.
                    
                    var job = _jobSystem.Schedule(
                        $"chunk_rebuild_{neighborCoord}",
                        async ct =>
                        {
                            var worldPos = ChunkUtils.ChunkCoordinatesToWorldPosition((int)neighborCoord.X, (int)neighborCoord.Y, (int)neighborCoord.Z);
                            var chunk = neighborGo.Chunk; // We already have the chunk, no need to await GetChunk
                            
                            var mesh = _meshBuilder.BuildMeshData(chunk, (chunkCoords) => 
                            {
                                var nPos = ChunkUtils.ChunkCoordinatesToWorldPosition(
                                    (int)chunkCoords.X, 
                                    (int)chunkCoords.Y, 
                                    (int)chunkCoords.Z
                                );
                                if (_chunkGenerator.TryGetCachedChunk(nPos, out var n)) return n;
                                return null;
                            });
                            
                            return new ChunkBuildResult(chunk, mesh);
                        },
                        JobPriority.Normal,
                        onComplete: null,
                        cancellationToken: default
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
            }
        }
    }
}
