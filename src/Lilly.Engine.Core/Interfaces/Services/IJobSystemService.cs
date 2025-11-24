using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services.Base;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Provides a worker-thread job system for executing work with priority support.
/// </summary>
public interface IJobSystemService : IJobSystemMetrics, ILillyService
{
    /// <summary>
    /// Schedules a synchronous job for execution.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="onComplete">Optional callback invoked when the job completes.</param>
    /// <returns>A job handle for tracking and cancellation.</returns>
    IJobHandle Schedule(IJob job, JobPriority priority = JobPriority.Normal, Action? onComplete = null);

    /// <summary>
    /// Schedules an asynchronous job for execution.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="onComplete">Optional callback invoked when the job completes.</param>
    /// <param name="cancellationToken">Token used to cancel the job execution.</param>
    /// <returns>A job handle for tracking and cancellation.</returns>
    IJobHandle Schedule(IAsyncJob job, JobPriority priority = JobPriority.Normal, Action? onComplete = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a synchronous job that produces a result.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by the job.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="onComplete">Optional callback invoked when the job completes with the result.</param>
    /// <returns>A job handle with result for tracking and cancellation.</returns>
    IJobHandle<TResult> Schedule<TResult>(IJob<TResult> job, JobPriority priority = JobPriority.Normal, Action<TResult>? onComplete = null);

    /// <summary>
    /// Schedules an asynchronous job that produces a result.
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by the job.</typeparam>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="onComplete">Optional callback invoked when the job completes with the result.</param>
    /// <param name="cancellationToken">Token used to cancel the job execution.</param>
    /// <returns>A job handle with result for tracking and cancellation.</returns>
    IJobHandle<TResult> Schedule<TResult>(IAsyncJob<TResult> job, JobPriority priority = JobPriority.Normal, Action<TResult>? onComplete = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the worker pool.
    /// </summary>
    /// <param name="workerCount">Number of worker threads to create.</param>
    void Initialize(int workerCount);

    /// <summary>
    /// Requests shutdown of the worker pool.
    /// </summary>
    void Shutdown();
}
