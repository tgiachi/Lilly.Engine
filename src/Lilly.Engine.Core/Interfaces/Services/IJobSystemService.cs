using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Provides a pool of worker threads for executing synchronous and asynchronous jobs.
/// </summary>
public interface IJobSystemService
{
    /// <summary>
    /// Initializes the worker pool.
    /// </summary>
    /// <param name="workerCount">Number of worker threads to create.</param>
    void Initialize(int workerCount);

    /// <summary>
    /// Schedules a synchronous job without waiting for completion.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    void Schedule(IJob job);

    /// <summary>
    /// Schedules an asynchronous job without waiting for completion.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    void Schedule(IAsyncJob job);

    /// <summary>
    /// Executes a synchronous job and returns a task that completes when the job finishes.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">Token used to cancel the execution before it starts.</param>
    /// <returns>A task that completes when the job finishes.</returns>
    Task ExecuteAsync(IJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a synchronous job that produces a result.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">Token used to cancel the execution before it starts.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A task that completes with the job result.</returns>
    Task<TResult> ExecuteAsync<TResult>(IJob<TResult> job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous job.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">Token propagated to the job execution.</param>
    /// <returns>A task that completes when the job finishes.</returns>
    Task ExecuteAsync(IAsyncJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous job that produces a result.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="cancellationToken">Token propagated to the job execution.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A task that completes with the job result.</returns>
    Task<TResult> ExecuteAsync<TResult>(IAsyncJob<TResult> job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests shutdown of the worker pool.
    /// </summary>
    void Shutdown();
}
