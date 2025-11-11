using Lilly.Engine.Core.Logging;
using Serilog.Events;

namespace Lilly.Engine.Debuggers;

/// <summary>
/// In-game log viewer with deduplication and filtering capabilities.
/// Similar to Unity's console window.
/// </summary>
public class LogViewer : IDisposable
{
    private readonly Dictionary<string, LogEntry> _logEntriesById = new();
    private readonly List<LogEntry> _logEntriesOrdered = new();
    private readonly Lock _lockObject = new();

    /// <summary>
    /// Gets the settings for filtering and display options.
    /// </summary>
    public LogViewerSettings Settings { get; } = new();

    /// <summary>
    /// Gets the total count of log entries (including duplicates).
    /// </summary>
    public int TotalLogCount { get; private set; }

    /// <summary>
    /// Gets the count of unique log entries.
    /// </summary>
    public int UniqueLogCount => _logEntriesOrdered.Count;

    /// <summary>
    /// Gets the count of errors.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Gets the count of warnings.
    /// </summary>
    public int WarningCount { get; private set; }

    /// <summary>
    /// Gets the count of information logs.
    /// </summary>
    public int InfoCount { get; private set; }

    /// <summary>
    /// Event raised when a new log entry is added or updated.
    /// </summary>
    public event EventHandler? OnLogsChanged;

    /// <summary>
    /// Initializes the log viewer and subscribes to the EventSink.
    /// </summary>
    public void Initialize()
    {
        EventSink.OnLogReceived += OnLogReceived;
    }

    /// <summary>
    /// Gets all log entries that match the current filters.
    /// </summary>
    public IReadOnlyList<LogEntry> GetFilteredLogs()
    {
        lock (_lockObject)
        {
            return _logEntriesOrdered
                   .Where(entry => Settings.MatchesFilter(entry))
                   .ToList();
        }
    }

    /// <summary>
    /// Gets all log entries (unfiltered).
    /// </summary>
    public IReadOnlyList<LogEntry> GetAllLogs()
    {
        lock (_lockObject)
        {
            return _logEntriesOrdered.ToList();
        }
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _logEntriesById.Clear();
            _logEntriesOrdered.Clear();
            TotalLogCount = 0;
            ErrorCount = 0;
            WarningCount = 0;
            InfoCount = 0;
        }

        OnLogsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets log statistics grouped by level.
    /// </summary>
    public Dictionary<LogEventLevel, int> GetStatistics()
    {
        lock (_lockObject)
        {
            return _logEntriesOrdered
                   .GroupBy(e => e.Level)
                   .ToDictionary(g => g.Key, g => g.Sum(e => e.Count));
        }
    }

    /// <summary>
    /// Exports logs to a text format.
    /// </summary>
    public string ExportToText(bool includeTimestamps = true)
    {
        lock (_lockObject)
        {
            var lines = _logEntriesOrdered.Select(
                entry =>
                {
                    var timestamp = includeTimestamps ? $"[{entry.FirstOccurrence:yyyy-MM-dd HH:mm:ss}] " : "";
                    var count = entry.IsCollapsed ? $"[{entry.Count}x] " : "";
                    var level = $"[{entry.Level}] ";
                    var source = entry.SourceContext != null ? $"[{entry.SourceContext}] " : "";

                    return $"{timestamp}{count}{level}{source}{entry.Message}";
                }
            );

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// Handles incoming log events from EventSink.
    /// </summary>
    private void OnLogReceived(object? sender, LogEventData logData)
    {
        lock (_lockObject)
        {
            var entry = new LogEntry(logData);
            var id = entry.Id;

            // Check if we already have this log entry (deduplication)
            if (_logEntriesById.TryGetValue(id, out var existingEntry))
            {
                // Update the existing entry
                existingEntry.IncrementCount(logData.Timestamp);
            }
            else
            {
                // Add new entry
                _logEntriesById[id] = entry;
                _logEntriesOrdered.Add(entry);

                // Trim old entries if we exceed max
                if (_logEntriesOrdered.Count > Settings.MaxEntries)
                {
                    var oldestEntry = _logEntriesOrdered[0];
                    _logEntriesOrdered.RemoveAt(0);
                    _logEntriesById.Remove(oldestEntry.Id);
                }
            }

            // Update counters
            TotalLogCount++;
            UpdateLevelCounters(logData.Level, 1);
        }

        OnLogsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Updates the level-specific counters.
    /// </summary>
    private void UpdateLevelCounters(LogEventLevel level, int delta)
    {
        switch (level)
        {
            case LogEventLevel.Error:
            case LogEventLevel.Fatal:
                ErrorCount += delta;

                break;
            case LogEventLevel.Warning:
                WarningCount += delta;

                break;
            case LogEventLevel.Information:
            case LogEventLevel.Debug:
            case LogEventLevel.Verbose:
                InfoCount += delta;

                break;
        }
    }

    /// <summary>
    /// Disposes the log viewer and unsubscribes from EventSink.
    /// </summary>
    public void Dispose()
    {
        EventSink.OnLogReceived -= OnLogReceived;
        GC.SuppressFinalize(this);
    }
}
