using Lilly.Engine.Core.Logging;
using Serilog.Events;

namespace Lilly.Engine.Data.Logger;

/// <summary>
/// Represents a log entry with deduplication support.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets the unique identifier for this log entry (hash of message + level + source).
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the log level.
    /// </summary>
    public LogEventLevel Level { get; }

    /// <summary>
    /// Gets the formatted log message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the source context (class name).
    /// </summary>
    public string? SourceContext { get; }

    /// <summary>
    /// Gets the exception if present.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp of the first occurrence.
    /// </summary>
    public DateTimeOffset FirstOccurrence { get; }

    /// <summary>
    /// Gets the timestamp of the last occurrence.
    /// </summary>
    public DateTimeOffset LastOccurrence { get; private set; }

    /// <summary>
    /// Gets the number of times this log has occurred.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Gets whether this log entry is collapsed (when count > 1).
    /// </summary>
    public bool IsCollapsed => Count > 1;

    /// <summary>
    /// Initializes a new log entry from log event data.
    /// </summary>
    public LogEntry(LogEventData logData)
    {
        Level = logData.Level;
        Message = logData.Message;
        SourceContext = logData.SourceContext;
        Exception = logData.Exception;
        FirstOccurrence = logData.Timestamp;
        LastOccurrence = logData.Timestamp;
        Count = 1;

        // Generate unique ID for deduplication
        Id = GenerateId(logData.Message, logData.Level, logData.SourceContext);
    }

    /// <summary>
    /// Increments the occurrence count and updates the last occurrence timestamp.
    /// </summary>
    public void IncrementCount(DateTimeOffset timestamp)
    {
        Count++;
        LastOccurrence = timestamp;
    }

    /// <summary>
    /// Generates a unique ID for deduplication based on message, level, and source.
    /// </summary>
    private static string GenerateId(string message, LogEventLevel level, string? sourceContext)
    {
        var hash = HashCode.Combine(message, level, sourceContext ?? string.Empty);
        return hash.ToString();
    }

    /// <summary>
    /// Gets the display text with count badge if collapsed.
    /// </summary>
    public string GetDisplayText()
    {
        return IsCollapsed ? $"[{Count}x] {Message}" : Message;
    }

    /// <summary>
    /// Gets a color associated with the log level.
    /// </summary>
    public (byte R, byte G, byte B, byte A) GetLevelColor()
    {
        return Level switch
        {
            LogEventLevel.Verbose => (128, 128, 128, 255),    // Gray
            LogEventLevel.Debug => (150, 150, 150, 255),      // Light Gray
            LogEventLevel.Information => (255, 255, 255, 255),// White
            LogEventLevel.Warning => (255, 255, 0, 255),      // Yellow
            LogEventLevel.Error => (255, 100, 100, 255),      // Red
            LogEventLevel.Fatal => (255, 0, 0, 255),          // Bright Red
            _ => (255, 255, 255, 255)
        };
    }
}
