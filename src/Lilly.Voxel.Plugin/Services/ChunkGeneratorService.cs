using System.Numerics;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Rendering.Core.Extensions;
using Lilly.Voxel.Plugin.Contexts;
using Lilly.Voxel.Plugin.Data;
using Lilly.Voxel.Plugin.Data.Cache;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Utils;
using Serilog;

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
    private readonly ChunkStreamingConfiguration _config;

    // Configuration
    private readonly bool _useJobSystem = true;

    // Metrics counters
    private long _totalChunksGenerated;
    private long _cacheHits;
    private long _cacheMisses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkGeneratorService" /> class.
    /// </summary>
    /// <param name="timerService">Timer service for cache management.</param>
    /// <param name="blockRegistry">Block registry for chunk generation.</param>
    /// <param name="jobSystemService">Job system for async chunk generation.</param>
    /// <param name="config">Chunk streaming configuration.</param>
    public ChunkGeneratorService(
        ITimerService timerService,
        IBlockRegistry blockRegistry,
        IJobSystemService jobSystemService,
        ChunkStreamingConfiguration config
    )
    {
        _blockRegistry = blockRegistry;
        _jobSystemService = jobSystemService;
        _config = config;

        // Initialize concurrency limit (cap at half logical cores to avoid pegging CPU)
        var maxConcurrentGenerations = Math.Max(1, Environment.ProcessorCount / 2);
        _generationSemaphore = new(maxConcurrentGenerations, maxConcurrentGenerations);
        _logger.Information(
            "Chunk generator initialized with max {MaxConcurrent} concurrent chunk generations",
            maxConcurrentGenerations
        );

        // Initialize noise generator
        InitializeNoiseGenerator();

        // Initialize cache with expiration time
        _chunkCache = new(timerService, TimeSpan.FromMinutes(5), _config.MaxCacheSizeChunks);
        _logger.Information("Chunk cache initialized with {Minutes} minute expiration", 5);
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
        get => _config.MaxCacheSizeChunks;
        set
        {
            var newValue = Math.Max(1, value);

            if (_config.MaxCacheSizeChunks == newValue)
            {
                return;
            }

            _config.MaxCacheSizeChunks = newValue;
            _chunkCache.SetCapacity(_config.MaxCacheSizeChunks);
            _logger.Information("Chunk cache capacity updated to {Capacity}", _config.MaxCacheSizeChunks);
        }
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
    /// Clears all cached chunks.
    /// </summary>
    public void ClearCache()
    {
        _logger.Information("Clearing chunk cache");
        _chunkCache.Clear();
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

    /// <inheritdoc />
    public void Dispose()
    {
        _chunkCache.Dispose();
        _pipelineLock.Dispose();
        _generationSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }

    public async Task GenerateInitialChunksAsync()
    {
        _logger.Information(
            "Generating initial chunks with radius {Radius} around position {Position}",
            _config.InitialChunkRadius,
            _config.InitialPosition
        );

        // var chunksToGenerate = new List<Vector3>();

        // // Normalize the initial position to chunk coordinates
        var centerChunkPos = ChunkUtils.NormalizeToChunkPosition(_config.InitialPosition);

        //
        // // Calculate all chunk positions to generate in a radius around the initial position
        // // Now including vertical layers (from minLayer to maxLayer)
        for (var y = _config.InitialChunkMinLayer; y <= _config.InitialChunkMaxLayer; y++)
        {
            for (var x = -_config.InitialChunkRadius; x <= _config.InitialChunkRadius; x++)
            {
                for (var z = -_config.InitialChunkRadius; z <= _config.InitialChunkRadius; z++)
                {
                    var chunkPos = new Vector3(
                        centerChunkPos.X + x * ChunkEntity.Size,
                        centerChunkPos.Y + y * ChunkEntity.Height,
                        centerChunkPos.Z + z * ChunkEntity.Size
                    );

                    //chunksToGenerate.Add(chunkPos);

                    GetChunkByWorldPosition(chunkPos);
                }
            }
        }
    }

    public IEnumerable<ChunkEntity> GetActiveChunks()
        => _chunkCache.GetAll();

    public async Task<ChunkEntity> GetChunkByWorldPosition(Vector3 position)
    {
        // Normalize position to chunk coordinates
        var chunkPosition = ChunkUtils.NormalizeToChunkPosition(position);

        // Try to get from cache first
        if (_chunkCache.TryGet(chunkPosition, out var cachedChunk) && cachedChunk != null)
        {
            Interlocked.Increment(ref _cacheHits);
            _logger.Verbose("Returning cached chunk at {Position}", chunkPosition);

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

        return GetChunkByWorldPosition(new(worldPosition.X, worldPosition.Y, worldPosition.Z));
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

    public Task ShutdownAsync()
        => Task.CompletedTask;

    public async Task StartAsync()
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

    public bool TryGetCachedChunk(Vector3 position, out ChunkEntity? chunk)
    {
        var chunkPosition = ChunkUtils.NormalizeToChunkPosition(position);

        return _chunkCache.TryGet(chunkPosition, out chunk);
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
            var chunk = new ChunkEntity(new(chunkPosition.X, chunkPosition.Y, chunkPosition.Z));

            // Create a thread-safe copy of the noise generator for this chunk
            var noiseGenerator = CreateNoiseGeneratorCopy();
            var context = new GeneratorContext(chunk, _blockRegistry, chunkPosition, noiseGenerator, Seed);

            _logger.Debug("Starting generation pipeline for chunk at {Position}", chunkPosition.ToHumanReadableString());

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
                    await step.ExecuteAsync(context);
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
            _logger.Debug("Chunk generation completed at {Position}", chunkPosition.ToHumanReadableString());

            return chunk;
        }
        finally
        {
            _generationSemaphore.Release();
        }
    }

    private async Task<ChunkEntity> GenerateChunkWrap(Vector3 chunkPosition)
    {
        var shouldUseJobSystem = _useJobSystem;

        if (shouldUseJobSystem)
        {
            var handle = _jobSystemService.Schedule(
                $"chunk_generation_{chunkPosition.ToHumanReadableString()}",
                _ => GenerateChunkAsync(chunkPosition)
            );

            return await handle.CompletionTask;
        }

        return await GenerateChunkAsync(chunkPosition);
    }

    /// <summary>
    /// Initializes the noise generator with the current seed.
    /// </summary>
    private void InitializeNoiseGenerator()
    {
        _noiseGenerator = new(Seed);
        _noiseGenerator.SetNoiseType(NoiseType.Perlin);

        // _noiseGenerator.SetFrequency((float)(Random.Shared.NextDouble() * 1000.0));
    }
}
