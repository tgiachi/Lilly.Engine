using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Primitives;
using Lilly.Voxel.Plugin.GameObjects.Environment;
using Lilly.Voxel.Plugin.Data.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Utils;
using Serilog;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

/// <summary>
/// Maintains the voxel world around the active camera by streaming chunks in/out and rendering environment effects.
/// </summary>
public class VoxelWorldGameObject : BaseGameObject3D, IDisposable
{
    private readonly RainEffectGameObject _rainEffect;
    private readonly SnowEffectGameObject _snowEffect;
    private readonly IChunkGeneratorService _chunkGeneratorService;
    private readonly IGameObjectFactory _gameObjectFactory;
    private readonly ICamera3dService _camera3dService;
    private readonly BlockOutlineGameObject _blockOutline;
    private readonly IJobSystemService _jobSystemService;
    private readonly ILogger _logger = Log.ForContext<VoxelWorldGameObject>();

    private readonly ConcurrentDictionary<ChunkCoordinates, ChunkGameObject> _activeChunks = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, byte> _requestedChunks = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, ChunkEntity> _prefetchedChunks = new();
    private readonly ConcurrentQueue<(ChunkCoordinates Coordinates, ChunkEntity Chunk)> _pendingChunks = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, byte> _pendingChunkCoordinates = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, ChunkFailureInfo> _chunkFailures = new();
    private readonly ConcurrentQueue<ChunkCoordinates> _neighborInvalidateQueue = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, byte> _queuedNeighborInvalidations = new();
    private readonly CancellationTokenSource _chunkCancellation = new();

    private static readonly float ChunkBoundingSphereRadius =
        0.5f * MathF.Sqrt(ChunkEntity.Size * ChunkEntity.Size * 2 + ChunkEntity.Height * ChunkEntity.Height);

    private static readonly ChunkCoordinates[] NeighborOffsets =
    [
        ChunkCoordinates.Left,
        ChunkCoordinates.Right,
        ChunkCoordinates.Down,
        ChunkCoordinates.Up,
        ChunkCoordinates.Backward,
        ChunkCoordinates.Forward
    ];

    private ChunkCoordinates _lastCenterCoordinates;
    private bool _disposed;
    private bool _showChunkBoundaries;

    public VoxelWorldGameObject(
        IGameObjectFactory gameObjectFactory,
        GraphicsDevice graphicsDevice,
        IChunkGeneratorService chunkGeneratorService,
        ICamera3dService camera3dService,
        IJobSystemService jobSystemService
    ) : base(graphicsDevice)
    {
        IgnoreFrustumCulling = true;

        _gameObjectFactory = gameObjectFactory ?? throw new ArgumentNullException(nameof(gameObjectFactory));
        _chunkGeneratorService = chunkGeneratorService ?? throw new ArgumentNullException(nameof(chunkGeneratorService));
        _camera3dService = camera3dService ?? throw new ArgumentNullException(nameof(camera3dService));
        _jobSystemService = jobSystemService ?? throw new ArgumentNullException(nameof(jobSystemService));

        _blockOutline = gameObjectFactory.Create<BlockOutlineGameObject>();

        _rainEffect = gameObjectFactory.Create<RainEffectGameObject>();
        _snowEffect = gameObjectFactory.Create<SnowEffectGameObject>();
        SkyGameObject = gameObjectFactory.Create<SkyGameObject>();

        IsRaining = false;
        IsSnowing = false;

        AddChild(SkyGameObject);
        AddChild(_blockOutline);
        AddChild(_rainEffect);
        AddChild(_snowEffect);
    }

    public SkyGameObject SkyGameObject { get; }

    public bool IsRaining
    {
        get => _rainEffect.IsVisible;
        set => _rainEffect.IsVisible = value;
    }

    public bool IsSnowing
    {
        get => _snowEffect.IsVisible;
        set => _snowEffect.IsVisible = value;
    }

    public int ChunkLoadDistance { get; set; } = 5;

    public int ChunkVerticalLoadDistance { get; set; } = 2;

    public int ChunkVerticalBelowLoadDistance { get; set; } = 1;

    public int ChunkGenerationDistance { get; set; } = 6;

    public int ChunkVerticalGenerationDistance { get; set; } = 3;

    public int ChunkVerticalBelowGenerationDistance { get; set; } = 1;

    public int MaxPendingChunkRequests { get; set; } = 64;

    public int MaxPrefetchedChunks { get; set; } = 128;

    public int MaxChunksToProcessPerFrame { get; set; } = 2;

    public int MaxChunkRequestsPerFrame { get; set; } = 12;

    public int MaxNeighborInvalidationsPerFrame { get; set; } = 1;

    public float ChunkProcessBudgetMs { get; set; } = 1.5f;

    public bool EnableFog { get; set; } = true;

    public Vector3D<float> FogColor { get; set; } = new Vector3D<float>(0.7f, 0.8f, 0.9f);

    public float FogStart { get; set; } = 48f;

    public float FogEnd { get; set; } = 160f;

    public Vector3D<float> AmbientLight { get; set; } = new Vector3D<float>(0.5f, 0.5f, 0.5f);

    public Vector3D<float> LightDirection { get; set; } = new Vector3D<float>(0.8f, 1.0f, 0.7f);

    public float LightIntensity { get; set; } = 1.5f;

    private float _waterTransparency = 0.1f;

    public float WaterTransparency
    {
        get => _waterTransparency;
        set
        {
            _waterTransparency = Math.Clamp(value, 0f, 1f);

            foreach (var chunk in _activeChunks.Values)
            {
                chunk.WaterTransparency = _waterTransparency;
            }
        }
    }

    public bool ShowChunkBoundaries
    {
        get => _showChunkBoundaries;
        set
        {
            if (_showChunkBoundaries == value)
            {
                return;
            }

            _showChunkBoundaries = value;

            foreach (var chunk in _activeChunks.Values)
            {
                chunk.ShowChunkBoundaries = value;
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (_disposed)
        {
            return;
        }

        var camera = _camera3dService.ActiveCamera;

        if (camera == null)
        {
            return;
        }

        UpdateChunkLoading(camera);
        ProcessPendingChunks();
        ProcessNeighborInvalidations();
    }

    private void UpdateChunkLoading(ICamera3D camera)
    {
        var centerVector = ChunkUtils.GetChunkCoordinates(camera.Position.ToSystem());
        var center = new ChunkCoordinates(
            (int)centerVector.X,
            (int)centerVector.Y,
            (int)centerVector.Z
        );

        var loadSet = new HashSet<ChunkCoordinates>();
        var candidates = new List<(ChunkCoordinates Coordinates, int Priority, bool WithinLoad)>();
        var horizontalGeneration = Math.Max(ChunkGenerationDistance, ChunkLoadDistance);
        var lowerGeneration = Math.Max(ChunkVerticalBelowGenerationDistance, ChunkVerticalBelowLoadDistance);
        var upperGeneration = Math.Max(ChunkVerticalGenerationDistance, ChunkVerticalLoadDistance);

        for (var y = -lowerGeneration; y <= upperGeneration; y++)
        {
            for (var x = -horizontalGeneration; x <= horizontalGeneration; x++)
            {
                for (var z = -horizontalGeneration; z <= horizontalGeneration; z++)
                {
                    var coords = new ChunkCoordinates(center.X + x, center.Y + y, center.Z + z);
                    var withinLoad = Math.Abs(x) <= ChunkLoadDistance &&
                                     Math.Abs(z) <= ChunkLoadDistance &&
                                     y >= -ChunkVerticalBelowLoadDistance &&
                                     y <= ChunkVerticalLoadDistance;

                    if (withinLoad)
                    {
                        loadSet.Add(coords);

                        if (_prefetchedChunks.TryRemove(coords, out var prefetchedChunk))
                        {
                            if (!_activeChunks.ContainsKey(coords))
                            {
                                AttachChunk(coords, prefetchedChunk);
                            }

                            continue;
                        }
                    }

                    if (_activeChunks.ContainsKey(coords) ||
                        _requestedChunks.ContainsKey(coords) ||
                        _pendingChunkCoordinates.ContainsKey(coords) ||
                        (!withinLoad && _prefetchedChunks.ContainsKey(coords)))
                    {
                        continue;
                    }

                    var priority = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);
                    candidates.Add((coords, priority, withinLoad));
                }
            }
        }

        var orderedCandidates = candidates
                                .OrderBy(c => c.WithinLoad ? 0 : 1)
                                .ThenBy(c => c.Priority)
                                .ThenBy(c => Math.Abs(c.Coordinates.Y - center.Y))
                                .ToList();

        var pendingRequests = _requestedChunks.Count;
        var requestsThisFrame = 0;

        foreach (var candidate in orderedCandidates)
        {
            if (!candidate.WithinLoad && pendingRequests >= MaxPendingChunkRequests)
            {
                break;
            }

            if (MaxChunkRequestsPerFrame > 0 && requestsThisFrame >= MaxChunkRequestsPerFrame)
            {
                break;
            }

            if (RequestChunkAsync(candidate.Coordinates))
            {
                pendingRequests++;
                requestsThisFrame++;
            }
        }

        RemoveChunksOutsideLoad(loadSet);
        CleanupPrefetchedChunks(loadSet);

        _lastCenterCoordinates = center;
    }

    private void RemoveChunksOutsideLoad(HashSet<ChunkCoordinates> loadSet)
    {
        foreach (var key in _activeChunks.Keys.ToList())
        {
            if (loadSet.Contains(key))
            {
                continue;
            }

            RemoveChunk(key);
        }
    }

    private void CleanupPrefetchedChunks(HashSet<ChunkCoordinates> loadSet)
    {
        if (_prefetchedChunks.Count <= MaxPrefetchedChunks)
        {
            return;
        }

        foreach (var coord in _prefetchedChunks.Keys.ToList())
        {
            if (loadSet.Contains(coord))
            {
                continue;
            }

            _prefetchedChunks.TryRemove(coord, out _);

            if (_prefetchedChunks.Count <= MaxPrefetchedChunks)
            {
                break;
            }
        }
    }

    private bool RequestChunkAsync(ChunkCoordinates coordinates)
    {
        if (IsInFailureCooldown(coordinates))
        {
            return false;
        }

        var worldOrigin = ChunkUtils.ChunkCoordinatesToWorldPosition(
            coordinates.X,
            coordinates.Y,
            coordinates.Z
        );

        var worldPosition = new Vector3(worldOrigin.X, worldOrigin.Y, worldOrigin.Z);

        // Optimization: Check if chunk is already cached before scheduling a job
        // This avoids overhead of creating jobs for chunks that are already generated
        if (_chunkGeneratorService.TryGetCachedChunk(worldPosition, out var cachedChunk) && cachedChunk != null)
        {
            if (_pendingChunkCoordinates.TryAdd(coordinates, 0))
            {
                _pendingChunks.Enqueue((coordinates, cachedChunk));
                return true;
            }
            return false;
        }

        if (!_requestedChunks.TryAdd(coordinates, 0))
        {
            return false;
        }

        var job = new ChunkGenerationJob(this, coordinates, worldPosition);
        _jobSystemService.Schedule(job, JobPriority.Normal);

        return true;
    }

    private async Task GenerateChunkAsync(ChunkCoordinates coordinates, Vector3 worldPosition, CancellationToken workerToken)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(workerToken, _chunkCancellation.Token);
            var token = linkedCts.Token;

            if (token.IsCancellationRequested)
            {
                return;
            }

            var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(worldPosition).ConfigureAwait(false);
            _chunkFailures.TryRemove(coordinates, out _);

            if (token.IsCancellationRequested || _disposed)
            {
                return;
            }

            if (!_pendingChunkCoordinates.TryAdd(coordinates, 0))
            {
                return;
            }

            _pendingChunks.Enqueue((coordinates, chunk));
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Chunk generation failed at {Coordinates}", coordinates);
            RegisterChunkFailure(coordinates, ex);
        }
        finally
        {
            _requestedChunks.TryRemove(coordinates, out _);
        }
    }

    private void ProcessPendingChunks()
    {
        var processed = 0;
        var budgetStart = Stopwatch.GetTimestamp();

        while (_pendingChunks.TryDequeue(out var pending))
        {
            _pendingChunkCoordinates.TryRemove(pending.Coordinates, out _);

            if (!IsWithinLoadDistance(pending.Coordinates))
            {
                _prefetchedChunks.AddOrUpdate(
                    pending.Coordinates,
                    pending.Chunk,
                    (_, _) => pending.Chunk
                );

                continue;
            }

            AttachChunk(pending.Coordinates, pending.Chunk);
            processed++;

            if (ShouldBreakChunkProcessing(processed, budgetStart))
            {
                break;
            }
        }
    }

    private bool IsWithinLoadDistance(ChunkCoordinates coordinates)
    {
        var dx = Math.Abs(coordinates.X - _lastCenterCoordinates.X);
        var dz = Math.Abs(coordinates.Z - _lastCenterCoordinates.Z);

        if (dx > ChunkLoadDistance || dz > ChunkLoadDistance)
        {
            return false;
        }

        var dy = coordinates.Y - _lastCenterCoordinates.Y;

        return dy >= 0
                   ? dy <= ChunkVerticalLoadDistance
                   : -dy <= ChunkVerticalBelowLoadDistance;
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        var currentCamera = _camera3dService.ActiveCamera;

        if (currentCamera == null)
        {
            yield break;
        }

        foreach (var gameObject in Children.EnumerateAsGeneric<IGameObject3D>())
        {
            bool shouldRender = gameObject switch
            {
                ChunkGameObject chunk => IsChunkInFrustum(chunk, currentCamera),
                _                     => currentCamera.IsInFrustum(gameObject)
            };

            if (!shouldRender)
            {
                continue;
            }

            foreach (var command in gameObject.Render(gameTime))
            {
                yield return command;
            }
        }
    }

    public static bool IsChunkInFrustum(ChunkGameObject gameObject, ICamera3D camera)
    {
        if (gameObject.IgnoreFrustumCulling)
        {
            return true;
        }

        var origin = gameObject.Transform.Position;
        var center = new Vector3D<float>(
            origin.X + ChunkEntity.Size * 0.5f,
            origin.Y + ChunkEntity.Height * 0.5f,
            origin.Z + ChunkEntity.Size * 0.5f
        );

        return camera.Frustum.Intersects(center, ChunkBoundingSphereRadius);
    }

    private void AttachChunk(ChunkCoordinates coordinates, ChunkEntity chunk)
    {
        if (_activeChunks.ContainsKey(coordinates))
        {
            return;
        }

        var chunkGameObject = _gameObjectFactory.Create<ChunkGameObject>();
        ApplyRenderSettings(chunkGameObject);

        // Create callback to retrieve neighboring chunks for face culling at chunk boundaries
        Func<ChunkCoordinates, ChunkEntity?> getNeighborChunk = (neighborCoords) =>
                                                                {
                                                                    if (_activeChunks.TryGetValue(
                                                                            neighborCoords,
                                                                            out var neighborChunkObject
                                                                        ))
                                                                    {
                                                                        return neighborChunkObject.Chunk;
                                                                    }

                                                                    return null;
                                                                };

        chunkGameObject.SetChunk(chunk, getNeighborChunk);

        var worldOrigin = ChunkUtils.ChunkCoordinatesToWorldPosition(
            coordinates.X,
            coordinates.Y,
            coordinates.Z
        );

        chunkGameObject.Transform.Position = new Vector3D<float>(
            worldOrigin.X,
            worldOrigin.Y,
            worldOrigin.Z
        );

        AddChild(chunkGameObject);
        _activeChunks[coordinates] = chunkGameObject;

        QueueNeighborInvalidations(coordinates);

        _logger.Information("Loaded chunk at {Coordinates}", coordinates);
    }

    private void RemoveChunk(ChunkCoordinates coordinates)
    {
        if (_activeChunks.TryRemove(coordinates, out var chunk))
        {
            Children.Remove(chunk);
            chunk.Dispose();
            _logger.Information("Unloaded chunk at {Coordinates}", coordinates);

            QueueNeighborInvalidations(coordinates);
        }
    }

    private bool IsInFailureCooldown(ChunkCoordinates coordinates)
    {
        if (_chunkFailures.TryGetValue(coordinates, out var failure))
        {
            var now = DateTime.UtcNow;
            var backoffSeconds = Math.Min(30, 1 + failure.Attempts * 2);

            if (now - failure.LastFailureUtc < TimeSpan.FromSeconds(backoffSeconds))
            {
                return true;
            }

            // Cooldown has expired - remove from failure tracking
            _chunkFailures.TryRemove(coordinates, out _);
        }

        return false;
    }

    private void RegisterChunkFailure(ChunkCoordinates coordinates, Exception ex)
    {
        var now = DateTime.UtcNow;

        var updated = _chunkFailures.AddOrUpdate(
            coordinates,
            _ => new ChunkFailureInfo(now, 1, ex.Message),
            (_, prev) => new ChunkFailureInfo(now, prev.Attempts + 1, ex.Message)
        );

        var backoffSeconds = Math.Min(30, 1 + updated.Attempts * 2);

        _logger.Warning(
            "Chunk {Coordinates} failed ({Attempts}x). Next retry in ~{DelaySeconds}s. Last error: {Error}",
            coordinates,
            updated.Attempts,
            backoffSeconds,
            ex.Message
        );
    }

    private void QueueNeighborInvalidations(ChunkCoordinates coordinates)
    {
        foreach (var offset in NeighborOffsets)
        {
            var neighborCoords = coordinates + offset;

            if (_queuedNeighborInvalidations.TryAdd(neighborCoords, 1))
            {
                _neighborInvalidateQueue.Enqueue(neighborCoords);
            }
        }
    }

    private void ProcessNeighborInvalidations()
    {
        var processed = 0;

        while (processed < MaxNeighborInvalidationsPerFrame &&
               _neighborInvalidateQueue.TryDequeue(out var coords))
        {
            _queuedNeighborInvalidations.TryRemove(coords, out _);

            if (_activeChunks.TryGetValue(coords, out var neighborChunk))
            {
                neighborChunk.InvalidateGeometry();
            }

            processed++;
        }
    }

    private void ApplyRenderSettings(ChunkGameObject chunk)
    {
        chunk.ShowChunkBoundaries = ShowChunkBoundaries;
        chunk.FogEnabled = EnableFog;
        chunk.FogColor = FogColor;
        chunk.FogStart = FogStart;
        chunk.FogEnd = FogEnd;
        chunk.AmbientLight = AmbientLight;
        chunk.LightDirection = LightDirection;
        chunk.LightIntensity = LightIntensity;
        chunk.WaterTransparency = WaterTransparency;
    }

    private bool ShouldBreakChunkProcessing(int processedCount, long budgetStartTimestamp)
    {
        if (MaxChunksToProcessPerFrame > 0 && processedCount >= MaxChunksToProcessPerFrame)
        {
            return true;
        }

        return BudgetExceeded(budgetStartTimestamp, ChunkProcessBudgetMs);
    }

    private static bool BudgetExceeded(long startTimestamp, double budgetMs)
    {
        if (budgetMs <= 0)
        {
            return false;
        }

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

        return elapsed.TotalMilliseconds >= budgetMs;
    }

    /// <summary>
    /// Gets a block at the specified world coordinates.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>The block ID at the position, or 0 (air) if the chunk is not loaded.</returns>
    public ushort GetBlock(int worldX, int worldY, int worldZ)
    {
        var chunkCoords = ChunkUtils.GetChunkCoordinates(new Vector3(worldX, worldY, worldZ));
        var coords = new ChunkCoordinates((int)chunkCoords.X, (int)chunkCoords.Y, (int)chunkCoords.Z);

        if (!_activeChunks.TryGetValue(coords, out var chunkGameObject) || chunkGameObject.Chunk == null)
        {
            return 0; // Return air if chunk not loaded
        }

        var (localX, localY, localZ) = ChunkUtils.GetLocalIndices(new Vector3(worldX, worldY, worldZ));

        return chunkGameObject.Chunk.GetBlock(localX, localY, localZ);
    }

    /// <summary>
    /// Places a block at the specified world coordinates, rebuilding the affected chunk(s).
    /// </summary>
    /// <param name="blockId">The block ID to place (0 = air).</param>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>True if the block was placed in a loaded chunk; otherwise false.</returns>
    public bool AddBlock(ushort blockId, int worldX, int worldY, int worldZ)
    {
        var chunkCoordsVec = ChunkUtils.GetChunkCoordinates(new Vector3(worldX, worldY, worldZ));
        var chunkCoords = new ChunkCoordinates((int)chunkCoordsVec.X, (int)chunkCoordsVec.Y, (int)chunkCoordsVec.Z);

        if (!_activeChunks.TryGetValue(chunkCoords, out var chunkGameObject) || chunkGameObject.Chunk == null)
        {
            return false;
        }

        var (localX, localY, localZ) = ChunkUtils.GetLocalIndices(new Vector3(worldX, worldY, worldZ));

        if (!ChunkUtils.IsValidLocalPosition(localX, localY, localZ))
        {
            return false;
        }

        chunkGameObject.Chunk.SetBlock(localX, localY, localZ, blockId);
        chunkGameObject.Chunk.IsMeshDirty = true;
        chunkGameObject.InvalidateGeometry();

        if (IsOnChunkBoundary(localX, localY, localZ))
        {
            QueueNeighborInvalidations(chunkCoords);
        }

        return true;
    }

    /// <summary>
    /// Places multiple blocks in bulk, rebuilding affected chunk meshes once per chunk.
    /// </summary>
    /// <param name="blocks">Array of block placements (world coordinates).</param>
    /// <returns>The number of blocks successfully placed.</returns>
    public int AddBlocks(BlockBulk[] blocks)
    {
        return ApplyBulkBlocks(blocks, bulk => (ushort)bulk.BlockId);
    }

    /// <summary>
    /// Removes a block at the specified world coordinates (sets it to air), rebuilding the affected chunk(s).
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>True if the block was removed in a loaded chunk; otherwise false.</returns>
    public bool RemoveBlock(int worldX, int worldY, int worldZ)
    {
        return AddBlock(0, worldX, worldY, worldZ);
    }

    /// <summary>
    /// Removes multiple blocks (sets them to air) in bulk, rebuilding affected chunk meshes once per chunk.
    /// </summary>
    /// <param name="blocks">Array of block positions to clear.</param>
    /// <returns>The number of blocks successfully removed.</returns>
    public int RemoveBlocks(BlockBulk[] blocks)
    {
        return ApplyBulkBlocks(blocks, _ => (ushort)0);
    }

    private int ApplyBulkBlocks(BlockBulk[] blocks, Func<BlockBulk, ushort> blockSelector)
    {
        int modified = 0;
        var chunksToUpdate = new Dictionary<ChunkCoordinates, (ChunkGameObject Chunk, bool BoundaryTouched)>();

        foreach (var block in blocks)
        {
            var pos = block.Position;
            var chunkCoordsVec = ChunkUtils.GetChunkCoordinates(new Vector3(pos.X, pos.Y, pos.Z));
            var chunkCoords = new ChunkCoordinates((int)chunkCoordsVec.X, (int)chunkCoordsVec.Y, (int)chunkCoordsVec.Z);

            if (!_activeChunks.TryGetValue(chunkCoords, out var chunkGameObject) || chunkGameObject.Chunk == null)
            {
                continue;
            }

            var (localX, localY, localZ) = ChunkUtils.GetLocalIndices(new Vector3(pos.X, pos.Y, pos.Z));

            if (!ChunkUtils.IsValidLocalPosition(localX, localY, localZ))
            {
                continue;
            }

            var blockId = blockSelector(block);
            chunkGameObject.Chunk.SetBlock(localX, localY, localZ, blockId);
            modified++;

            var boundaryTouched = IsOnChunkBoundary(localX, localY, localZ);

            if (chunksToUpdate.TryGetValue(chunkCoords, out var info))
            {
                chunksToUpdate[chunkCoords] = (info.Chunk, info.BoundaryTouched || boundaryTouched);
            }
            else
            {
                chunksToUpdate[chunkCoords] = (chunkGameObject, boundaryTouched);
            }
        }

        foreach (var entry in chunksToUpdate)
        {
            var chunk = entry.Value.Chunk;

            if (chunk.Chunk != null)
            {
                chunk.Chunk.IsMeshDirty = true;
            }

            chunk.InvalidateGeometry();

            if (entry.Value.BoundaryTouched)
            {
                QueueNeighborInvalidations(entry.Key);
            }
        }

        return modified;
    }

    private static bool IsOnChunkBoundary(int x, int y, int z)
    {
        return x == 0 ||
               x == ChunkEntity.Size - 1 ||
               z == 0 ||
               z == ChunkEntity.Size - 1 ||
               y == 0 ||
               y == ChunkEntity.Height - 1;
    }

    /// <summary>
    /// Debug method: Tests if there are any loaded chunks with blocks
    /// </summary>
    public void DebugLogBlocksInfo()
    {
        _logger.Information("=== Voxel World Debug Info ===");
        _logger.Information("Active chunks: {Count}", _activeChunks.Count);

        if (_activeChunks.Count == 0)
        {
            _logger.Warning("No chunks loaded!");

            return;
        }

        foreach (var chunk in _activeChunks.Values.Take(3))
        {
            if (chunk?.Chunk == null)
                continue;

            var solidBlockCount = 0;

            foreach (var blockId in chunk.Chunk.Blocks)
            {
                if (blockId != 0)
                    solidBlockCount++;
            }

            _logger.Information(
                "Chunk at {Position}: {SolidBlocks} solid blocks out of {Total}",
                chunk.Transform.Position,
                solidBlockCount,
                chunk.Chunk.Blocks.Length
            );
        }
    }

    /// <summary>
    /// Performs raycasting from a ray and returns the first solid block hit.
    /// Uses voxel traversal algorithm (DDA-based) for accurate block detection.
    /// </summary>
    /// <param name="ray">The ray to cast.</param>
    /// <param name="maxDistance">Maximum distance to raycast.</param>
    /// <param name="blockPosition">The position of the hit block (if any).</param>
    /// <param name="skipFirstBlock">If true, skips the starting voxel (useful if camera is inside a block).</param>
    /// <returns>True if a block was hit, false otherwise.</returns>
    public bool Raycast(Ray ray, float maxDistance, out Vector3D<int> blockPosition, bool skipFirstBlock = true)
    {
        blockPosition = default;

        var origin = ray.Origin;
        var direction = Vector3D.Normalize(ray.Direction);

        // Current voxel position
        var voxel = new Vector3D<int>(
            (int)MathF.Floor(origin.X),
            (int)MathF.Floor(origin.Y),
            (int)MathF.Floor(origin.Z)
        );

        // Calculate step direction (1 or -1 for each axis)
        var step = new Vector3D<int>(
            direction.X >= 0 ? 1 : -1,
            direction.Y >= 0 ? 1 : -1,
            direction.Z >= 0 ? 1 : -1
        );

        // Calculate t parameter for each axis (distance to next voxel boundary)
        var tMax = new Vector3D<float>(
            direction.X != 0 ? (voxel.X + (direction.X > 0 ? 1 : 0) - origin.X) / direction.X : float.MaxValue,
            direction.Y != 0 ? (voxel.Y + (direction.Y > 0 ? 1 : 0) - origin.Y) / direction.Y : float.MaxValue,
            direction.Z != 0 ? (voxel.Z + (direction.Z > 0 ? 1 : 0) - origin.Z) / direction.Z : float.MaxValue
        );

        // Calculate t delta (distance between voxel boundaries)
        var tDelta = new Vector3D<float>(
            direction.X != 0 ? 1.0f / MathF.Abs(direction.X) : float.MaxValue,
            direction.Y != 0 ? 1.0f / MathF.Abs(direction.Y) : float.MaxValue,
            direction.Z != 0 ? 1.0f / MathF.Abs(direction.Z) : float.MaxValue
        );

        // Current distance along ray
        float t = 0;
        bool firstVoxel = true;

        while (t < maxDistance)
        {
            // Check current voxel (skip first if requested)
            if (!firstVoxel || !skipFirstBlock)
            {
                var block = GetBlock(voxel.X, voxel.Y, voxel.Z);

                if (block != 0)
                {
                    blockPosition = voxel;

                    return true;
                }
            }
            firstVoxel = false;

            // Find the next voxel to check (step along the axis with smallest t)
            if (tMax.X < tMax.Y && tMax.X < tMax.Z)
            {
                t = tMax.X;
                voxel.X += step.X;
                tMax.X += tDelta.X;
            }
            else if (tMax.Y < tMax.Z)
            {
                t = tMax.Y;
                voxel.Y += step.Y;
                tMax.Y += tDelta.Y;
            }
            else
            {
                t = tMax.Z;
                voxel.Z += step.Z;
                tMax.Z += tDelta.Z;
            }
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _chunkCancellation.Cancel();
        _chunkCancellation.Dispose();

        foreach (var chunk in _activeChunks.Values)
        {
            Children.Remove(chunk);
            chunk.Dispose();
        }

        _activeChunks.Clear();

        while (_pendingChunks.TryDequeue(out _)) { }

        GC.SuppressFinalize(this);
    }

    private sealed class ChunkGenerationJob : IAsyncJob
    {
        private readonly VoxelWorldGameObject _owner;
        private readonly ChunkCoordinates _coordinates;
        private readonly Vector3 _worldPosition;

        public ChunkGenerationJob(VoxelWorldGameObject owner, ChunkCoordinates coordinates, Vector3 worldPosition)
        {
            _owner = owner;
            _coordinates = coordinates;
            _worldPosition = worldPosition;
        }

        public string Name => $"chunk_generation_{_coordinates.X}_{_coordinates.Y}_{_coordinates.Z}";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return _owner.GenerateChunkAsync(_coordinates, _worldPosition, cancellationToken);
        }
    }

    public bool RemoveCurrentBlock()
    {
        if (_blockOutline.TargetBlockPosition != null)
        {
            var pos = _blockOutline.TargetBlockPosition.Value;

            return RemoveBlock(pos.X, pos.Y, pos.Z);
        }

        return false;
    }

    private readonly record struct ChunkFailureInfo(DateTime LastFailureUtc, int Attempts, string? LastError);
}
