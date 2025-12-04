using Lilly.Engine.Core.Data.Dispatchers;

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
    /// Gets the total number of actions executed since initialization.
    /// </summary>
    long TotalActionsExecuted { get; }

    /// <summary>
    /// Gets the number of actions that failed with exceptions.
    /// </summary>
    long FailedActionsCount { get; }

    /// <summary>
    /// Gets the average execution time of completed actions in milliseconds.
    /// </summary>
    double AverageExecutionTimeMs { get; }

    /// <summary>
    /// Gets a snapshot of the most recently finished actions.
    /// </summary>
    IReadOnlyList<ActionExecutionRecord> RecentActions { get; }

    /// <summary>
    /// Enqueues an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to enqueue.</param>
    void EnqueueAction(Action action);

    /// <summary>
    /// Enqueues a named action to be executed on the main thread.
    /// </summary>
    /// <param name="name">The name of the action for diagnostics.</param>
    /// <param name="action">The action to enqueue.</param>
    void EnqueueAction(string name, Action action);
}
