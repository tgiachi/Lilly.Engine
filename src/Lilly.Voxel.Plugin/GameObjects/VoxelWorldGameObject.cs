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
using Lilly.Voxel.Plugin.GameObjects.Environment;
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
    private readonly IJobSystemService _jobSystemService;
    private readonly ILogger _logger = Log.ForContext<VoxelWorldGameObject>();

    private readonly ConcurrentDictionary<ChunkCoordinates, ChunkGameObject> _activeChunks = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, byte> _requestedChunks = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, byte> _prefetchedChunks = new();
    private readonly ConcurrentQueue<(ChunkCoordinates Coordinates, ChunkEntity Chunk)> _pendingChunks = new();
    private readonly ConcurrentDictionary<ChunkCoordinates, byte> _pendingChunkCoordinates = new();
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

        _rainEffect = gameObjectFactory.Create<RainEffectGameObject>();
        _snowEffect = gameObjectFactory.Create<SnowEffectGameObject>();
        SkyGameObject = gameObjectFactory.Create<SkyGameObject>();

        IsRaining = false;
        IsSnowing = false;

        AddChild(SkyGameObject);
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

    public int MaxPrefetchedChunks { get; set; } = 512;

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

    public float WaterTransparency { get; set; } = 0.1f;

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
                        _prefetchedChunks.TryRemove(coords, out _);
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
        if (!_requestedChunks.TryAdd(coordinates, 0))
        {
            return false;
        }

        var worldOrigin = ChunkUtils.ChunkCoordinatesToWorldPosition(
            coordinates.X,
            coordinates.Y,
            coordinates.Z
        );

        var worldPosition = new Vector3(worldOrigin.X, worldOrigin.Y, worldOrigin.Z);
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
                _prefetchedChunks.TryAdd(pending.Coordinates, 0);

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
}
