using Lilly.Engine.Core.Interfaces.Jobs;
using Lilly.Engine.Core.Interfaces.Services.Base;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Provides a pool of worker threads for executing synchronous and asynchronous jobs.
/// </summary>
public interface IJobSystemService : IJobSystemMetrics, ILillyService
{

    /// <summary>
    /// Executes a synchronous job and returns a task that completes when the job finishes.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution before it starts.</param>
    /// <returns>A job handle and completion task.</returns>
    IJobHandle ExecuteAsync(IJob job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a synchronous job that produces a result.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution before it starts.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A job handle and completion task with result.</returns>
    IJobHandle<TResult> ExecuteAsync<TResult>(IJob<TResult> job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);


    /// <summary>
    /// Executes an asynchronous job.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token propagated to the job execution.</param>
    /// <returns>A job handle and completion task.</returns>
    IJobHandle ExecuteAsync(IAsyncJob job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous job that produces a result.
    /// </summary>
    /// <param name="job">The job to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token propagated to the job execution.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A job handle and completion task with result.</returns>
    IJobHandle<TResult> ExecuteAsync<TResult>(IAsyncJob<TResult> job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);


    /// <summary>
    /// Executes a task-returning function that produces a result.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="taskFactory">Function that creates and returns the task to execute, receives cancellation token.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A job handle and completion task with result.</returns>
    IJobHandle<TResult> ExecuteTaskAsync<TResult>(string name, Func<CancellationToken, Task<TResult>> taskFactory, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a task-returning function that produces a result.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="taskFactory">Function that creates and returns the task to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A job handle and completion task with result.</returns>
    IJobHandle<TResult> ExecuteTaskAsync<TResult>(string name, Func<Task<TResult>> taskFactory, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a synchronous action as a job.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <returns>A job handle and completion task.</returns>
    IJobHandle ExecuteAsync(string name, Action action, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a synchronous action as a job that produces a result.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A job handle and completion task with result.</returns>
    IJobHandle<TResult> ExecuteAsync<TResult>(string name, Func<TResult> action, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous function as a job.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="asyncAction">The async function to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <returns>A job handle and completion task.</returns>
    IJobHandle ExecuteAsync(string name, Func<Task> asyncAction, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an asynchronous function as a job that produces a result.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="asyncAction">The async function to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    /// <returns>A job handle and completion task with result.</returns>
    IJobHandle<TResult> ExecuteAsync<TResult>(string name, Func<Task<TResult>> asyncAction, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);


    /// <summary>
    /// Schedules a synchronous job without waiting for completion.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <returns>A job handle for tracking.</returns>
    IJobHandle Schedule(IJob job, JobPriority priority = JobPriority.Normal);

    /// <summary>
    /// Schedules an asynchronous job without waiting for completion.
    /// </summary>
    /// <param name="job">The job to enqueue.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the job execution.</param>
    /// <returns>A job handle for tracking.</returns>
    IJobHandle Schedule(IAsyncJob job, JobPriority priority = JobPriority.Normal, CancellationToken cancellationToken = default);


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
