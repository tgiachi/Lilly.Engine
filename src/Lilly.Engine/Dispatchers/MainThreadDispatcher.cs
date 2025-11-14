using System.Collections.Concurrent;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Serilog;

namespace Lilly.Engine.Dispatchers;

/// <summary>
/// Dispatcher specifically designed for main thread execution with thread-safety guarantees.
/// Handles actions from multiple threads and executes them in parallel on the main thread.
/// </summary>
public class MainThreadDispatcher : IMainThreadDispatcher, IDisposable
{
    private readonly int _mainThreadId;

    private readonly ConcurrentQueue<Action> _actionQueue = new();

    private readonly ILogger _logger = Log.ForContext<MainThreadDispatcher>();

    private readonly IGraphicRenderer _renderer;

    /// <summary>
    /// Initializes a new instance of the MainThreadDispatcher class.
    /// </summary>
    /// <param name="renderer">The graphic renderer to hook updates to.</param>
    public MainThreadDispatcher(IGraphicRenderer renderer)
    {
        _renderer = renderer;
        _mainThreadId = Environment.CurrentManagedThreadId;
        _renderer.Update += Update;
    }

    public int QueuedActionCount => _actionQueue.Count;

    public int ConcurrentActionCount => 3;

    /// <summary>
    /// Disposes the MainThreadDispatcher and releases resources.
    /// </summary>
    public void Dispose()
    {
        _renderer.Update -= Update;
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

        _actionQueue.Enqueue(action);
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

        var actionsToExecute = new List<Action>();

        for (var i = 0; i < ConcurrentActionCount; i++)
        {
            if (_actionQueue.TryDequeue(out var action))
            {
                actionsToExecute.Add(action);
            }
            else
            {
                break;
            }
        }

        foreach (var action in actionsToExecute)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "Error executing action on main thread from thread: {ThreadId}",
                    Environment.CurrentManagedThreadId
                );
            }
        }
    }
}
