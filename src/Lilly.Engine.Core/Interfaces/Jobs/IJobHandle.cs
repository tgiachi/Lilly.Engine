namespace Lilly.Engine.Core.Interfaces.Jobs;

/// <summary>
/// Represents a handle to a scheduled job that allows monitoring and cancellation.
/// </summary>
public interface IJobHandle
{
    /// <summary>
    /// Gets a task that completes when the job finishes execution.
    /// </summary>
    Task CompletionTask { get; }

    /// <summary>
    /// Gets a unique identifier for this job handle.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the name of the job.
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Gets the priority of the job.
    /// </summary>
    JobPriority Priority { get; }

    /// <summary>
    /// Gets whether the job has completed (either successfully, faulted, or cancelled).
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Attempts to cancel the job. Returns true if cancellation was initiated, false if the job has already completed.
    /// </summary>
    /// <returns>True if cancellation was initiated, false otherwise.</returns>
    bool Cancel();
}

/// <summary>
/// Represents a handle to a scheduled job that produces a result.
/// </summary>
/// <typeparam name="TResult">The type of result produced by the job.</typeparam>
public interface IJobHandle<TResult> : IJobHandle
{
    /// <summary>
    /// Gets a task that completes with the job result.
    /// </summary>
    new Task<TResult> CompletionTask { get; }
}
