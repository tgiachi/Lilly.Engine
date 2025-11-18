using System.Diagnostics;
using System.Numerics;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Contexts;
using Lilly.Voxel.Plugin.Data.Cache;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Utils;
using Serilog;
using Silk.NET.Maths;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Manages chunk generation using a configurable pipeline and time-based cache.
/// </summary>
public class ChunkGeneratorService : IChunkGeneratorService, IDisposable
{
    private int _seed = Random.Shared.Next();

    public int Seed
    {
        get => _seed;
        set
        {
            _seed = value;
            InitializeNoiseGenerator();
        }
    }

    private readonly ILogger _logger = Log.ForContext<ChunkGeneratorService>();
    private readonly ChunkCache _chunkCache;
    private readonly List<IGeneratorStep> _pipeline = [];
    private readonly ReaderWriterLockSlim _pipelineLock = new();
    private readonly SemaphoreSlim _generationSemaphore;
    private FastNoiseLite _noiseGenerator;
    private readonly IBlockRegistry _blockRegistry;

    private readonly IJobSystemService _jobSystemService;

    private readonly int _initialChunkRadius = 5;
    private Vector3 _initialPosition = Vector3.Zero;

    // Configuration
    private readonly int _maxConcurrentGenerations;
    private int _maxCachedChunks = 512;
    private bool _useJobSystem;

    // Metrics counters
    private long _totalChunksGenerated;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGeneratorService"/> class.
    /// </summary>
    /// <param name="timerService">Timer service for cache management.</param>
    /// <param name="config">Chunk generator configuration.</param>
    public ChunkGeneratorService(
        ITimerService timerService,
        IBlockRegistry blockRegistry,
        IJobSystemService jobSystemService
    )
    {
        _blockRegistry = blockRegistry;
        _jobSystemService = jobSystemService;
        ArgumentNullException.ThrowIfNull(timerService);

        // Initialize concurrency limit (use CPU count * 2 as a reasonable default)
        _maxConcurrentGenerations = Math.Max(Environment.ProcessorCount * 2, 4);
        _generationSemaphore = new SemaphoreSlim(_maxConcurrentGenerations, _maxConcurrentGenerations);
        _logger.Information(
            "Chunk generator initialized with max {MaxConcurrent} concurrent chunk generations",
            _maxConcurrentGenerations
        );

        // Initialize noise generator
        InitializeNoiseGenerator();

        // Initialize cache with expiration time
        _chunkCache = new ChunkCache(timerService, TimeSpan.FromMinutes(10), _maxCachedChunks);
        _logger.Information("Chunk cache initialized with {Minutes} minute expiration", 10);
    }

    /// <summary>
    /// Initializes the noise generator with the current seed.
    /// </summary>
    private void InitializeNoiseGenerator()
    {
        _noiseGenerator = new FastNoiseLite(Seed);
        _noiseGenerator.SetNoiseType(NoiseType.Perlin);

        // _noiseGenerator.SetFrequency((float)(Random.Shared.NextDouble() * 1000.0));
    }

    /// <summary>
    /// Creates a thread-safe copy of the noise generator for parallel generation.
    /// </summary>
    /// <returns>A new FastNoiseLite instance with the same configuration.</returns>
    private FastNoiseLite CreateNoiseGeneratorCopy()
    {
        var copy = new FastNoiseLite(Seed);
        copy.SetNoiseType(NoiseType.Perlin);

        //copy.SetFrequency((float)(Random.Shared.NextDouble() * 1000.0));
        //copy.SetFrequency(0.01f);
        return copy;
    }

    public async Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position)
    {
        _logger.Debug("Requested chunk at world position {Position}", position);

        // Normalize position to chunk coordinates
        var chunkPosition = ChunkUtils.NormalizeToChunkPosition(position);

        // Try to get from cache first
        if (_chunkCache.TryGet(chunkPosition, out var cachedChunk) && cachedChunk != null)
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.Debug("Returning cached chunk at {Position}", chunkPosition);

            return cachedChunk;
        }

        // Cache miss
        Interlocked.Increment(ref _cacheMisses);

        // Generate new chunk
        _logger.Information("Generating new chunk at {Position}", chunkPosition);
        var chunk = await GenerateChunkWrap(chunkPosition);

        // Cache the generated chunk
        _chunkCache.Set(chunkPosition, chunk);

        return chunk;
    }

    public Task<ChunkEntity> GetChunkByWorldPosition(int chunkX, int chunkY, int chunkZ)
    {
        var worldPosition = ChunkUtils.ChunkCoordinatesToWorldPosition(chunkX, chunkY, chunkZ);

        return GetChunkByWorldPosition(new Vector3(worldPosition.X, worldPosition.Y, worldPosition.Z));
    }

    public async Task<IEnumerable<ChunkEntity>> GetChunksByPositions(IEnumerable<Vector3> positions)
    {
        var positionList = positions as IList<Vector3> ?? positions.ToList();

        _logger.Debug("Requested chunks for {Count} positions", positionList.Count);

        // Get all chunks in parallel
        var tasks = positionList.Select(GetChunkByWorldPosition);
        var chunks = await Task.WhenAll(tasks);

        _logger.Debug("Returned {Count} chunks", chunks.Length);

        return chunks;
    }

    public async Task GenerateInitialChunksAsync()
    {
        var startTime = Stopwatch.GetTimestamp();
        _logger.Information(
            "Generating initial chunks with radius {Radius} around position {Position}",
            _initialChunkRadius,
            _initialPosition
        );

        var chunksToGenerate = new List<Vector3>();

        // Normalize the initial position to chunk coordinates
        var centerChunkPos = ChunkUtils.NormalizeToChunkPosition(_initialPosition);

        // Calculate all chunk positions to generate in a radius around the initial position
        for (int x = -_initialChunkRadius; x <= _initialChunkRadius; x++)
        {
            for (int z = -_initialChunkRadius; z <= _initialChunkRadius; z++)
            {
                var chunkPos = new Vector3(
                    centerChunkPos.X + (x * ChunkEntity.Size),
                    centerChunkPos.Y,
                    centerChunkPos.Z + (z * ChunkEntity.Size)
                );
                chunksToGenerate.Add(chunkPos);
            }
        }

        _logger.Information("Generating {Count} initial chunks", chunksToGenerate.Count);

        // Generate chunks in parallel
        var tasks = chunksToGenerate.Select(GetChunkByWorldPosition);
        await Task.WhenAll(tasks);

        var elapsed = Stopwatch.GetElapsedTime(startTime);
        _logger.Information(
            "Initial chunk generation completed. Generated {Count} chunks in {Elapsed:F2}ms",
            chunksToGenerate.Count,
            elapsed.TotalMilliseconds
        );
    }

    private async Task<ChunkEntity> GenerateChunkWrap(Vector3 chunkPosition)
    {
        if (_useJobSystem)
        {
            var handle = _jobSystemService.ExecuteTaskAsync(
                "ChunkGeneration",
                _ => GenerateChunkAsync(chunkPosition)
            );

            return await handle.CompletionTask;
        }

        return await GenerateChunkAsync(chunkPosition);
    }

    /// <summary>
    /// Generates a new chunk at the specified position using the generation pipeline.
    /// </summary>
    /// <param name="chunkPosition">The normalized chunk position.</param>
    /// <returns>The generated chunk.</returns>
    private async Task<ChunkEntity> GenerateChunkAsync(Vector3 chunkPosition)
    {
        // Limit concurrent chunk generation to prevent resource exhaustion
        await _generationSemaphore.WaitAsync();

        try
        {
            var chunk = new ChunkEntity(new Vector3D<float>(chunkPosition.X, chunkPosition.Y, chunkPosition.Z));

            // Create a thread-safe copy of the noise generator for this chunk
            var noiseGenerator = CreateNoiseGeneratorCopy();
            var context = new GeneratorContext(chunk, _blockRegistry, chunkPosition, noiseGenerator, Seed);

            _logger.Debug("Starting generation pipeline for chunk at {Position}", chunkPosition);

            // Get a snapshot of the pipeline with read lock to allow concurrent execution
            IGeneratorStep[] pipelineSteps;
            _pipelineLock.EnterReadLock();

            try
            {
                pipelineSteps = _pipeline.ToArray();
            }
            finally
            {
                _pipelineLock.ExitReadLock();
            }

            // Execute each step in the pipeline
            foreach (var step in pipelineSteps)
            {
                _logger.Debug("Executing generation step: {StepName}", step.Name);

                try
                {
                    // context.CloudAreas.Clear();
                    // await step.ExecuteAsync(context);
                    //
                    // if (context.CloudAreas.Count > 0)
                    // {
                    //     _logger.Debug(
                    //         "Step '{StepName}' identified {CloudCount} cloud areas in chunk at {Position}",
                    //         step.Name,
                    //         context.CloudAreas.Count,
                    //         chunkPosition
                    //     );

                    // var cloudGameObject = SquidVoxEngineContext.GetService<RenderLayerCollection>()
                    //     .GetComponent<CloudsGameObject>();
                    //
                    // if (cloudGameObject != null)
                    // {
                    //     foreach (var area in context.CloudAreas)
                    //     {
                    //         cloudGameObject.AddCloud(new Cloud(area.Position, area.Size));
                    //     }
                    // }
                    // }
                }

                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        "Error during generation step '{StepName}' at chunk {Position}",
                        step.Name,
                        chunkPosition
                    );

                    throw;
                }
            }

            Interlocked.Increment(ref _totalChunksGenerated);
            _logger.Debug("Chunk generation completed at {Position}", chunkPosition);

            return chunk;
        }
        finally
        {
            _generationSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the current number of cached chunks.
    /// </summary>
    public int CachedChunkCount => _chunkCache.Count;

    /// <summary>
    /// Gets or sets the maximum number of cached chunks retained in memory.
    /// </summary>
    public int MaxCachedChunks
    {
        get => _maxCachedChunks;
        set
        {
            var newValue = Math.Max(1, value);

            if (_maxCachedChunks == newValue)
            {
                return;
            }

            _maxCachedChunks = newValue;
            _chunkCache.SetCapacity(_maxCachedChunks);
            _logger.Information("Chunk cache capacity updated to {Capacity}", _maxCachedChunks);
        }
    }

    /// <summary>
    /// Clears all cached chunks.
    /// </summary>
    public void ClearCache()
    {
        _logger.Information("Clearing chunk cache");
        _chunkCache.Clear();
    }

    /// <summary>
    /// Adds a generation step to the pipeline.
    /// </summary>
    /// <param name="generationStep">The generator step to add.</param>
    public void AddGeneratorStep(IGeneratorStep generationStep)
    {
        ArgumentNullException.ThrowIfNull(generationStep);

        _pipelineLock.EnterWriteLock();

        try
        {
            _pipeline.Add(generationStep);
            _logger.Information(
                "Added generator step '{StepName}' to pipeline. Total steps: {Count}",
                generationStep.Name,
                _pipeline.Count
            );
        }
        finally
        {
            _pipelineLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes a generation step from the pipeline by name.
    /// </summary>
    /// <param name="stepName">The name of the step to remove.</param>
    /// <returns>True if the step was removed; otherwise, false.</returns>
    public bool RemoveGeneratorStep(string stepName)
    {
        _pipelineLock.EnterWriteLock();

        try
        {
            var step = _pipeline.FirstOrDefault(s => s.Name == stepName);

            if (step != null)
            {
                _pipeline.Remove(step);
                _logger.Information(
                    "Removed generator step '{StepName}' from pipeline. Remaining steps: {Count}",
                    stepName,
                    _pipeline.Count
                );

                return true;
            }

            _logger.Warning("Generator step '{StepName}' not found in pipeline", stepName);

            return false;
        }
        finally
        {
            _pipelineLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets all generator steps in the pipeline.
    /// </summary>
    public IReadOnlyList<IGeneratorStep> GetGeneratorSteps()
    {
        _pipelineLock.EnterReadLock();

        try
        {
            return _pipeline.ToList().AsReadOnly();
        }
        finally
        {
            _pipelineLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Clears all generator steps from the pipeline.
    /// </summary>
    public void ClearGeneratorSteps()
    {
        _pipelineLock.EnterWriteLock();

        try
        {
            _pipeline.Clear();
            _logger.Information("Cleared all generator steps from pipeline");
        }
        finally
        {
            _pipelineLock.ExitWriteLock();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting ChunkGeneratorService");

        try
        {
            await GenerateInitialChunksAsync();
            _logger.Information("ChunkGeneratorService started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start ChunkGeneratorService");

            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _logger.Information("Disposing ChunkGeneratorService");
        _chunkCache.Dispose();
        _pipelineLock.Dispose();
        _generationSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}
