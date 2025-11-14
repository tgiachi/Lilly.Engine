using System.Collections.Concurrent;

namespace Lilly.Engine.Rendering.Core.Data.Diagnostics;

/// <summary>
/// Provides diagnostic information about the render pipeline performance and statistics.
/// </summary>
public class RenderPipelineDiagnostics
{
    private readonly ConcurrentDictionary<string, LayerStatistics> _layerStats = new();
    private readonly Queue<FrameStatistics> _frameHistory = new(120); // Keep last 120 frames (2 seconds at 60 FPS)
    private readonly Lock _lockObject = new();

    /// <summary>
    /// Gets the total number of render commands processed in the current frame.
    /// </summary>
    public int TotalCommandsThisFrame { get; private set; }

    /// <summary>
    /// Gets the average number of commands per frame over the history window.
    /// </summary>
    public double AverageCommandsPerFrame { get; private set; }

    /// <summary>
    /// Gets the peak number of commands in a single frame.
    /// </summary>
    public int PeakCommandsPerFrame { get; private set; }

    /// <summary>
    /// Gets the total number of frames processed.
    /// </summary>
    public long TotalFrames { get; private set; }

    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    public double CurrentFPS { get; private set; }

    /// <summary>
    /// Gets statistics for each render layer.
    /// </summary>
    public IReadOnlyDictionary<string, LayerStatistics> LayerStatistics => _layerStats;

    /// <summary>
    /// Statistics for a single frame.
    /// </summary>
    private struct FrameStatistics
    {
        public long FrameNumber;
        public int TotalCommands;
        public double DeltaTimeMs;
    }

    /// <summary>
    /// Records the start of a new frame.
    /// </summary>
    public void BeginFrame()
    {
        TotalCommandsThisFrame = 0;

        foreach (var stat in _layerStats.Values)
        {
            stat.CommandsThisFrame = 0;
        }
    }

    /// <summary>
    /// Records the end of a frame and updates statistics.
    /// </summary>
    /// <param name="deltaTimeMs">The time taken for this frame in milliseconds.</param>
    public void EndFrame(double deltaTimeMs)
    {
        lock (_lockObject)
        {
            TotalFrames++;

            // Update peak
            if (TotalCommandsThisFrame > PeakCommandsPerFrame)
            {
                PeakCommandsPerFrame = TotalCommandsThisFrame;
            }

            // Add to history
            _frameHistory.Enqueue(
                new()
                {
                    FrameNumber = TotalFrames,
                    TotalCommands = TotalCommandsThisFrame,
                    DeltaTimeMs = deltaTimeMs
                }
            );

            // Keep only last 120 frames
            if (_frameHistory.Count > 120)
            {
                _frameHistory.Dequeue();
            }

            // Calculate averages based on frame history
            if (_frameHistory.Count > 0)
            {
                AverageCommandsPerFrame = _frameHistory.Average(f => f.TotalCommands);

                // Calculate FPS as average over frame history for smoother display
                var avgDeltaTimeMs = _frameHistory.Average(f => f.DeltaTimeMs);

                if (avgDeltaTimeMs > 0)
                {
                    CurrentFPS = 1000.0 / avgDeltaTimeMs;
                }
            }

            // Update layer averages
            foreach (var stat in _layerStats.Values)
            {
                if (stat.TotalFrames > 0)
                {
                    stat.AverageCommandsPerFrame = (double)stat.TotalCommandsProcessed / stat.TotalFrames;
                }
            }
        }
    }

    /// <summary>
    /// Gets a summary of the current statistics as a formatted string.
    /// </summary>
    /// <returns>A formatted string containing diagnostic information.</returns>
    public string GetSummary()
    {
        lock (_lockObject)
        {
            var summary = "=== Render Pipeline Diagnostics ===\n";
            summary += $"Total Frames: {TotalFrames:N0}\n";
            summary += $"Current FPS: {CurrentFPS:F2}\n";
            summary += $"Commands This Frame: {TotalCommandsThisFrame:N0}\n";
            summary += $"Average Commands/Frame: {AverageCommandsPerFrame:F2}\n";
            summary += $"Peak Commands/Frame: {PeakCommandsPerFrame:N0}\n\n";

            summary += "=== Layer Statistics ===\n";

            foreach (var (layerName, stat) in _layerStats.OrderBy(kvp => kvp.Key))
            {
                summary += $"[{layerName}]\n";
                summary += $"  Current: {stat.CommandsThisFrame:N0} | ";
                summary += $"Avg: {stat.AverageCommandsPerFrame:F2} | ";
                summary += $"Peak: {stat.PeakCommands:N0} | ";
                summary += $"Total: {stat.TotalCommandsProcessed:N0}\n";
            }

            return summary;
        }
    }

    /// <summary>
    /// Records commands processed by a specific layer.
    /// </summary>
    /// <param name="layerName">The name of the render layer.</param>
    /// <param name="commandCount">The number of commands processed.</param>
    public void RecordLayerCommands(string layerName, int layerOrder, int commandCount)
    {
        TotalCommandsThisFrame += commandCount;

        var stat = _layerStats.GetOrAdd(layerName, _ => new(layerName));
        stat.CommandsThisFrame = commandCount;
        stat.TotalCommandsProcessed += commandCount;
        stat.TotalFrames++;
        stat.LayerOrder = layerOrder;

        if (commandCount > stat.PeakCommands)
        {
            stat.PeakCommands = commandCount;
        }
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            TotalCommandsThisFrame = 0;
            AverageCommandsPerFrame = 0;
            PeakCommandsPerFrame = 0;
            TotalFrames = 0;
            CurrentFPS = 0;
            _layerStats.Clear();
            _frameHistory.Clear();
        }
    }
}

/// <summary>
/// Statistics for a specific render layer.
/// </summary>
public class LayerStatistics
{
    /// <summary>
    /// Initializes a new instance of the LayerStatistics class.
    /// </summary>
    /// <param name="layerName">The name of the render layer.</param>
    public LayerStatistics(string layerName)
        => LayerName = layerName;

    /// <summary>
    /// Gets the name of the render layer.
    /// </summary>
    public string LayerName { get; }

    /// <summary>
    /// Gets or sets the order of the layer in the render pipeline.
    /// </summary>
    public int LayerOrder { get; set; }

    /// <summary>
    /// Gets or sets the number of commands processed in the current frame.
    /// </summary>
    public int CommandsThisFrame { get; set; }

    /// <summary>
    /// Gets or sets the average number of commands per frame.
    /// </summary>
    public double AverageCommandsPerFrame { get; set; }

    /// <summary>
    /// Gets or sets the peak number of commands in a single frame.
    /// </summary>
    public int PeakCommands { get; set; }

    /// <summary>
    /// Gets or sets the total number of commands processed by this layer.
    /// </summary>
    public long TotalCommandsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of frames processed by this layer.
    /// </summary>
    public long TotalFrames { get; set; }
}
