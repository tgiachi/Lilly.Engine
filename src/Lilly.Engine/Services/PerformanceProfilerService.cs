using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Context;
using TrippyGL;

namespace Lilly.Engine.Services;

/// <summary>
/// Service for collecting and managing performance metrics
/// </summary>
public class PerformanceProfilerService : IPerformanceProfilerService
{
    private const int HistorySize = 120;        // 2 seconds at 60 FPS
    private const int AveragingWindowSize = 30; // Average over 30 frames
    private readonly Queue<double> _drawTimeHistory = new();
    private readonly Queue<double> _drawTimeWindow = new();
    private readonly Queue<double> _fpsHistory = new();
    private readonly Queue<double> _frameTimeHistory = new();
    private readonly Queue<double> _frameTimeWindow = new();

    private readonly Lock _lock = new();
    private readonly Queue<double> _updateTimeHistory = new();
    private readonly Queue<double> _updateTimeWindow = new();

    private DateTime _lastUpdate = DateTime.UtcNow;
    private double _minFrameTime = double.MaxValue;
    private GraphicsDevice _graphicsDevice;

    /// <summary>
    /// Gets the current frame rate (FPS)
    /// </summary>
    public double CurrentFps => CurrentFrameTime > 0 ? 1000.0 / CurrentFrameTime : 0.0;

    /// <summary>
    /// Gets the average frame rate over the last few seconds
    /// </summary>
    public double AverageFps => AverageFrameTime > 0 ? 1000.0 / AverageFrameTime : 0.0;

    /// <summary>
    /// Gets the current frame time in milliseconds
    /// </summary>
    public double CurrentFrameTime { get; private set; }

    /// <summary>
    /// Gets the average frame time in milliseconds
    /// </summary>
    public double AverageFrameTime
    {
        get
        {
            lock (_lock)
            {
                return _frameTimeWindow.Count > 0 ? _frameTimeWindow.Average() : 0.0;
            }
        }
    }

    /// <summary>
    /// Gets the current update time in milliseconds
    /// </summary>
    public double CurrentUpdateTime { get; private set; }

    /// <summary>
    /// Gets the current draw time in milliseconds
    /// </summary>
    public double CurrentDrawTime { get; private set; }

    /// <summary>
    /// Gets the average draw time in milliseconds
    /// </summary>
    public double AverageDrawTime
    {
        get
        {
            lock (_lock)
            {
                return _drawTimeWindow.Count > 0 ? _drawTimeWindow.Average() : 0.0;
            }
        }
    }

    /// <summary>
    /// Gets the minimum frame time recorded
    /// </summary>
    public double MinFrameTime => _minFrameTime == double.MaxValue ? 0.0 : _minFrameTime;

    /// <summary>
    /// Gets the maximum frame time recorded
    /// </summary>
    public double MaxFrameTime { get; private set; }

    /// <summary>
    /// Gets the current memory usage in MB
    /// </summary>
    public double MemoryUsageMb => GC.GetTotalMemory(false) / (1024.0 * 1024.0);

    /// <summary>
    /// Gets the total number of frames processed
    /// </summary>
    public long TotalFrames { get; private set; }

    /// <summary>
    /// Gets performance history for graphing
    /// </summary>
    public IReadOnlyList<double> FrameTimeHistory
    {
        get
        {
            lock (_lock)
            {
                return _frameTimeHistory.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets update time history for graphing
    /// </summary>
    public IReadOnlyList<double> UpdateTimeHistory
    {
        get
        {
            lock (_lock)
            {
                return _updateTimeHistory.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets draw time history for graphing
    /// </summary>
    public IReadOnlyList<double> DrawTimeHistory
    {
        get
        {
            lock (_lock)
            {
                return _drawTimeHistory.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets FPS history for graphing
    /// </summary>
    public IReadOnlyList<double> FpsHistory
    {
        get
        {
            lock (_lock)
            {
                return _fpsHistory.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets a summary of current performance metrics
    /// </summary>
    /// <returns>Dictionary of metric name to value</returns>
    public Dictionary<string, object> GetMetricsSummary()
    {
        lock (_lock)
        {
            var metrics = new Dictionary<string, object>
            {
                ["Current FPS"] = Math.Round(CurrentFps, 1),
                ["Average FPS"] = Math.Round(AverageFps, 1),
                ["Current Frame Time"] = Math.Round(CurrentFrameTime, 2),
                ["Average Frame Time"] = Math.Round(AverageFrameTime, 2),
                ["Min Frame Time"] = Math.Round(MinFrameTime, 2),
                ["Max Frame Time"] = Math.Round(MaxFrameTime, 2),
                ["Current Draw Time"] = Math.Round(CurrentDrawTime, 2),
                ["Average Draw Time"] = Math.Round(AverageDrawTime, 2),
                ["Memory Usage (MB)"] = Math.Round(MemoryUsageMb, 2),
                ["Total Frames"] = TotalFrames
            };

            return metrics;
        }
    }

    public PerformanceProfilerService(RenderContext renderContext)
    {
        _graphicsDevice = renderContext.GraphicsDevice;
        renderContext.Renderer.OnUpdate += RendererOnOnUpdate;
        renderContext.Renderer.OnRender += RendererOnOnRender;
    }

    private void RendererOnOnRender(GameTime gameTime)
    {
        UpdateDrawTime(gameTime);
    }

    private void RendererOnOnUpdate(GameTime gameTime)
    {
        Update(gameTime);
    }

    /// <summary>
    /// Resets all performance metrics
    /// </summary>
    public void ResetMetrics()
    {
        lock (_lock)
        {
            _frameTimeHistory.Clear();
            _updateTimeHistory.Clear();
            _drawTimeHistory.Clear();
            _fpsHistory.Clear();
            _frameTimeWindow.Clear();
            _updateTimeWindow.Clear();
            _drawTimeWindow.Clear();

            TotalFrames = 0;
            CurrentFrameTime = 0.0;
            CurrentUpdateTime = 0.0;
            CurrentDrawTime = 0.0;
            _minFrameTime = double.MaxValue;
            MaxFrameTime = 0.0;
        }
    }

    public void Update(GameTime gameTime)
    {
        UpdateFrameTime(gameTime);
        UpdateUpdateTime(gameTime);
    }

    /// <summary>
    /// Updates draw timing metrics
    /// </summary>
    /// <param name="drawTime">Draw time in milliseconds</param>
    public void UpdateDrawTime(GameTime gameTime)
    {
        var drawTime = gameTime.ElapsedGameTime;

        lock (_lock)
        {
            CurrentDrawTime = drawTime;

            // Add to history
            _drawTimeHistory.Enqueue(drawTime);

            if (_drawTimeHistory.Count > HistorySize)
            {
                _drawTimeHistory.Dequeue();
            }

            // Add to averaging window
            _drawTimeWindow.Enqueue(drawTime);

            if (_drawTimeWindow.Count > AveragingWindowSize)
            {
                _drawTimeWindow.Dequeue();
            }
        }

    }

    /// <summary>
    /// Updates frame timing metrics
    /// </summary>
    /// <param name="frameTime">Frame time in milliseconds</param>
    public void UpdateFrameTime(GameTime gameTime)
    {
        var frameTime = gameTime.ElapsedGameTime;

        lock (_lock)
        {
            CurrentFrameTime = frameTime;
            TotalFrames++;

            // Update min/max
            if (frameTime < _minFrameTime)
            {
                _minFrameTime = frameTime;
            }

            if (frameTime > MaxFrameTime)
            {
                MaxFrameTime = frameTime;
            }

            // Add to history
            _frameTimeHistory.Enqueue(frameTime);

            if (_frameTimeHistory.Count > HistorySize)
            {
                _frameTimeHistory.Dequeue();
            }

            // Add to averaging window
            _frameTimeWindow.Enqueue(frameTime);

            if (_frameTimeWindow.Count > AveragingWindowSize)
            {
                _frameTimeWindow.Dequeue();
            }

            // Calculate and store FPS
            var currentFps = frameTime > 0 ? 1000.0 / frameTime : 0.0;
            _fpsHistory.Enqueue(currentFps);

            if (_fpsHistory.Count > HistorySize)
            {
                _fpsHistory.Dequeue();
            }
        }
    }

    /// <summary>
    /// Updates update timing metrics
    /// </summary>
    /// <param name="updateTime">Update time in milliseconds</param>
    public void UpdateUpdateTime(GameTime gameTime)
    {
        var updateTime = gameTime.ElapsedGameTime;

        lock (_lock)
        {
            CurrentUpdateTime = updateTime;

            // Add to history
            _updateTimeHistory.Enqueue(updateTime);

            if (_updateTimeHistory.Count > HistorySize)
            {
                _updateTimeHistory.Dequeue();
            }

            // Add to averaging window
            _updateTimeWindow.Enqueue(updateTime);

            if (_updateTimeWindow.Count > AveragingWindowSize)
            {
                _updateTimeWindow.Dequeue();
            }
        }
    }
}
