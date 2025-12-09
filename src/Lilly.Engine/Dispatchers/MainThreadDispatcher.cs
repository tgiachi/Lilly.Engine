using System.Collections.Concurrent;
using System.Diagnostics;
using Lilly.Engine.Core.Data.Dispatchers;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Serilog;

namespace Lilly.Engine.Dispatchers;

/// <summary>
/// Dispatcher specifically designed for main thread execution with thread-safety guarantees.
/// Handles actions from multiple threads and executes them in parallel on the main thread.
/// </summary>
public class MainThreadDispatcher : IMainThreadDispatcher, IDisposable
{
    private readonly int _mainThreadId;

    private readonly ConcurrentQueue<NamedAction> _actionQueue = new();

    private readonly ILogger _logger = Log.ForContext<MainThreadDispatcher>();

    private readonly IGraphicRenderer _renderer;

    private readonly List<ActionExecutionRecord> _recentActions = new(50);
    private readonly Lock _metricsLock = new();

    private long _totalActionsExecuted;
    private long _failedActionsCount;
    private double _totalExecutionTimeMs;

    private readonly record struct NamedAction(string Name, Action Action);

    /// <summary>
    /// Initializes a new instance of the MainThreadDispatcher class.
    /// </summary>
    /// <param name="renderer">The graphic renderer to hook updates to.</param>
    public MainThreadDispatcher(IGraphicRenderer renderer)
    {
        _renderer = renderer;
        _mainThreadId = Environment.CurrentManagedThreadId;
        _renderer.OnUpdate += Update;
    }

    public int QueuedActionCount => _actionQueue.Count;

    public int ConcurrentActionCount => 3;

    public long TotalActionsExecuted => _totalActionsExecuted;

    public long FailedActionsCount => _failedActionsCount;

    public double AverageExecutionTimeMs
        => _totalActionsExecuted > 0
               ? _totalExecutionTimeMs / _totalActionsExecuted
               : 0.0;

    public IReadOnlyList<ActionExecutionRecord> RecentActions
    {
        get
        {
            lock (_metricsLock)
            {
                return _recentActions.ToList();
            }
        }
    }

    /// <summary>
    /// Disposes the MainThreadDispatcher and releases resources.
    /// </summary>
    public void Dispose()
    {
        _renderer.OnUpdate -= Update;
        _actionQueue.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to enqueue.</param>
    public void EnqueueAction(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _actionQueue.Enqueue(new("Unnamed Action", action));
    }

    /// <summary>
    /// Enqueues a named action to be executed on the main thread.
    /// </summary>
    /// <param name="name">The name of the action for diagnostics.</param>
    /// <param name="action">The action to enqueue.</param>
    public void EnqueueAction(string name, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        _actionQueue.Enqueue(new(name.ToSnakeCase(), action));
    }

    /// <summary>
    /// Executes queued actions on the main thread.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public void Update(GameTime gameTime)
    {
        if (Environment.CurrentManagedThreadId != _mainThreadId)
        {
            _logger.Warning(
                "MainThreadDispatcher Update called from non-main thread: {ThreadId}",
                Environment.CurrentManagedThreadId
            );

            return;
        }

        var actionsToExecute = new List<NamedAction>();

        for (var i = 0; i < ConcurrentActionCount; i++)
        {
            if (_actionQueue.TryDequeue(out var namedAction))
            {
                actionsToExecute.Add(namedAction);
            }
            else
            {
                break;
            }
        }

        foreach (var namedAction in actionsToExecute)
        {
            var stopwatch = Stopwatch.StartNew();
            var status = ActionExecutionStatus.Succeeded;

            try
            {
                namedAction.Action();
            }
            catch (Exception ex)
            {
                status = ActionExecutionStatus.Failed;
                Interlocked.Increment(ref _failedActionsCount);

                _logger.Error(
                    ex,
                    "Error executing action '{ActionName}' on main thread",
                    namedAction.Name
                );
            }
            finally
            {
                stopwatch.Stop();
                var durationMs = stopwatch.Elapsed.TotalMilliseconds;

                Interlocked.Increment(ref _totalActionsExecuted);
                _totalExecutionTimeMs += durationMs;

                var record = new ActionExecutionRecord(
                    namedAction.Name,
                    status,
                    durationMs,
                    DateTime.UtcNow
                );

                lock (_metricsLock)
                {
                    _recentActions.Add(record);

                    if (_recentActions.Count > 50)
                    {
                        _recentActions.RemoveAt(0);
                    }
                }
            }
        }
    }
}
