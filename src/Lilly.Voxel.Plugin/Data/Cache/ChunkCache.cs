using System.Numerics;
using Lilly.Engine.Core.Interfaces.Services;
using Serilog;
using Squid.Engine.World.Voxels.Primitives;

namespace Lilly.Voxel.Plugin.Data.Cache;

/// <summary>
/// Manages a time-based cache for active chunks to avoid regenerating frequently accessed chunks.
/// </summary>
public class ChunkCache
{
    private readonly ILogger _logger = Log.ForContext<ChunkCache>();
    private readonly Dictionary<Vector3, CacheEntry> _cache = new();
    private readonly LinkedList<Vector3> _lruList = new();
    private readonly Lock _syncRoot = new();
    private readonly TimeSpan _expirationTime;
    private readonly ITimerService _timerService;
    private int _maxCapacity;
    private string? _cleanupTimerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkCache"/> class.
    /// </summary>
    /// <param name="timerService">Timer service for scheduling cache cleanup.</param>
    /// <param name="expirationTime">Time after which inactive chunks are removed from cache.</param>
    /// <param name="maxCapacity">Maximum number of chunks to keep cached before evicting the least recently used.</param>
    public ChunkCache(ITimerService timerService, TimeSpan expirationTime, int maxCapacity)
    {
        _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
        _expirationTime = expirationTime;
        _maxCapacity = Math.Max(1, maxCapacity);

        // Register cleanup timer to run every minute
        _cleanupTimerId = _timerService.RegisterTimer(
            "ChunkCacheCleanup",
            intervalInMs: 60000, // 60 seconds
            callback: CleanupExpiredEntries,
            delayInMs: 60000,
            repeat: true
        );

        _logger.Information("ChunkCache initialized with cleanup timer running every minute");
    }

    /// <summary>
    /// Tries to get a chunk from the cache.
    /// </summary>
    /// <param name="position">The world position of the chunk.</param>
    /// <param name="chunk">The cached chunk, if found.</param>
    /// <returns>True if the chunk was found in cache; otherwise, false.</returns>
    public bool TryGet(Vector3 position, out ChunkEntity? chunk)
    {
        lock (_syncRoot)
        {
            if (_cache.TryGetValue(position, out var entry))
            {
                entry.LastAccessTime = DateTime.UtcNow;
                MoveToFront(entry);
                chunk = entry.Chunk;

                _logger.Debug("Cache hit for chunk at {Position}", position);

                return true;
            }
        }

        chunk = null;
        _logger.Debug("Cache miss for chunk at {Position}", position);

        return false;
    }

    /// <summary>
    /// Adds or updates a chunk in the cache.
    /// </summary>
    /// <param name="position">The world position of the chunk.</param>
    /// <param name="chunk">The chunk to cache.</param>
    public void Set(Vector3 position, ChunkEntity chunk)
    {
        lock (_syncRoot)
        {
            if (_cache.TryGetValue(position, out var existing))
            {
                existing.Chunk = chunk;
                existing.LastAccessTime = DateTime.UtcNow;
                MoveToFront(existing);
            }
            else
            {
                var entry = new CacheEntry(chunk)
                {
                    LastAccessTime = DateTime.UtcNow
                };

                entry.Node = _lruList.AddFirst(position);
                _cache[position] = entry;
                TrimToCapacity();
            }

            _logger.Debug("Cached chunk at {Position}. Total cached chunks: {Count}", position, _cache.Count);
        }
    }

    /// <summary>
    /// Removes a chunk from the cache.
    /// </summary>
    /// <param name="position">The world position of the chunk to remove.</param>
    /// <returns>True if the chunk was removed; otherwise, false.</returns>
    public bool Remove(Vector3 position)
    {
        bool removed = false;

        lock (_syncRoot)
        {
            removed = RemoveInternal(position);
            if (removed)
            {
                _logger.Debug("Removed chunk at {Position} from cache", position);
            }
        }

        return removed;
    }

    /// <summary>
    /// Gets the number of chunks currently in the cache.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Gets the current maximum capacity of the cache.
    /// </summary>
    public int Capacity
    {
        get
        {
            lock (_syncRoot)
            {
                return _maxCapacity;
            }
        }
    }

    /// <summary>
    /// Sets the maximum capacity of the cache and evicts entries if necessary.
    /// </summary>
    /// <param name="capacity">New maximum capacity.</param>
    public void SetCapacity(int capacity)
    {
        lock (_syncRoot)
        {
            var newCapacity = Math.Max(1, capacity);

            if (_maxCapacity == newCapacity)
            {
                return;
            }

            _maxCapacity = newCapacity;
            TrimToCapacity();
        }
    }

    /// <summary>
    /// Clears all entries from the cache.
    /// </summary>
    public void Clear()
    {
        lock (_syncRoot)
        {
            var count = _cache.Count;
            _cache.Clear();
            _lruList.Clear();
            _logger.Information("Cleared {Count} chunks from cache", count);
        }
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        lock (_syncRoot)
        {
            if (_cache.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var expiredEntries = new List<Vector3>();

            foreach (var kvp in _cache)
            {
                if (now - kvp.Value.LastAccessTime > _expirationTime)
                {
                    expiredEntries.Add(kvp.Key);
                }
            }

            if (expiredEntries.Count == 0)
            {
                return;
            }

            _logger.Debug("Cleaning up {Count} expired chunks from cache", expiredEntries.Count);

            foreach (var position in expiredEntries)
            {
                RemoveInternal(position);
            }

            _logger.Information(
                "Removed {Count} expired chunks. Remaining: {Remaining}",
                expiredEntries.Count,
                _cache.Count
            );
        }
    }

    /// <summary>
    /// Represents a cached chunk with its access metadata.
    /// </summary>
    private sealed class CacheEntry
    {
        public CacheEntry(ChunkEntity chunk)
        {
            Chunk = chunk;
        }

        public ChunkEntity Chunk { get; set; }

        public DateTime LastAccessTime { get; set; }

        public LinkedListNode<Vector3>? Node { get; set; }
    }

    private void MoveToFront(CacheEntry entry)
    {
        if (entry.Node == null)
        {
            return;
        }

        _lruList.Remove(entry.Node);
        _lruList.AddFirst(entry.Node);
    }

    private void TrimToCapacity()
    {
        while (_cache.Count > _maxCapacity && _lruList.Last != null)
        {
            var node = _lruList.Last;
            if (node == null)
            {
                break;
            }

            if (RemoveInternal(node.Value))
            {
                _logger.Debug("Evicted chunk at {Position} due to capacity limit", node.Value);
            }
        }
    }

    private bool RemoveInternal(Vector3 position)
    {
        if (!_cache.TryGetValue(position, out var entry))
        {
            return false;
        }

        if (entry.Node != null)
        {
            _lruList.Remove(entry.Node);
            entry.Node = null;
        }

        _cache.Remove(position);
        return true;
    }

    /// <summary>
    /// Disposes resources used by the cache.
    /// </summary>
    public void Dispose()
    {
        if (_cleanupTimerId != null)
        {
            _timerService.UnregisterTimer(_cleanupTimerId);
            _cleanupTimerId = null;
        }

        Clear();
    }
}
