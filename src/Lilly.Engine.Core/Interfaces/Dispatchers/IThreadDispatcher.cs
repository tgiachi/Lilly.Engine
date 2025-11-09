namespace Lilly.Engine.Core.Interfaces.Dispatchers;

/// <summary>
/// Defines the contract for dispatching actions to a specific thread with queue management.
/// </summary>
public interface IThreadDispatcher
{
    /// <summary>
    /// Gets the number of actions queued for execution.
    /// </summary>
    int QueuedActionCount { get; }

    /// <summary>
    /// Gets the number of actions currently being executed concurrently.
    /// </summary>
    int ConcurrentActionCount { get; }

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to enqueue.</param>
    void EnqueueAction(Action action);
}
