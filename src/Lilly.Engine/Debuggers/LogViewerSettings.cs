using Serilog.Events;

namespace Lilly.Engine.Debuggers;

/// <summary>
/// Settings and filters for the log viewer.
/// </summary>
public class LogViewerSettings
{
    /// <summary>
    /// Gets or sets whether Verbose logs are shown.
    /// </summary>
    public bool ShowVerbose { get; set; } = false;

    /// <summary>
    /// Gets or sets whether Debug logs are shown.
    /// </summary>
    public bool ShowDebug { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Information logs are shown.
    /// </summary>
    public bool ShowInformation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Warning logs are shown.
    /// </summary>
    public bool ShowWarning { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Error logs are shown.
    /// </summary>
    public bool ShowError { get; set; } = true;

    /// <summary>
    /// Gets or sets whether Fatal logs are shown.
    /// </summary>
    public bool ShowFatal { get; set; } = true;

    /// <summary>
    /// Gets or sets the search filter text.
    /// </summary>
    public string SearchFilter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to show collapsed logs (deduplicated).
    /// </summary>
    public bool ShowCollapsed { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of log entries to keep.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to auto-scroll to the bottom when new logs arrive.
    /// </summary>
    public bool AutoScroll { get; set; } = true;

    /// <summary>
    /// Checks if a log level is currently filtered in.
    /// </summary>
    public bool IsLevelVisible(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => ShowVerbose,
            LogEventLevel.Debug => ShowDebug,
            LogEventLevel.Information => ShowInformation,
            LogEventLevel.Warning => ShowWarning,
            LogEventLevel.Error => ShowError,
            LogEventLevel.Fatal => ShowFatal,
            _ => true
        };
    }

    /// <summary>
    /// Checks if a log entry matches the current filters.
    /// </summary>
    public bool MatchesFilter(LogEntry entry)
    {
        // Check level filter
        if (!IsLevelVisible(entry.Level))
        {
            return false;
        }

        // Check search filter
        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            var searchLower = SearchFilter.ToLowerInvariant();
            var matchesMessage = entry.Message.Contains(searchLower, StringComparison.OrdinalIgnoreCase);
            var matchesSource = entry.SourceContext?.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ?? false;

            if (!matchesMessage && !matchesSource)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Resets all filters to default values.
    /// </summary>
    public void ResetFilters()
    {
        ShowVerbose = false;
        ShowDebug = true;
        ShowInformation = true;
        ShowWarning = true;
        ShowError = true;
        ShowFatal = true;
        SearchFilter = string.Empty;
    }
}
