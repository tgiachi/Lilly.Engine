namespace Lilly.Engine.Core.Interfaces.Jobs;

/// <summary>
/// Represents a synchronous job executed by the job system.
/// </summary>
public interface IJob
{
    /// <summary>
    /// Gets the job name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the job on a worker thread.
    /// </summary>
    void Execute();
}

/// <summary>
/// Represents a synchronous job that produces a result.
/// </summary>
/// <typeparam name="TResult">Type of the produced result.</typeparam>
public interface IJob<out TResult>
{
    /// <summary>
    /// Gets the job name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the job and returns the result.
    /// </summary>
    /// <returns>The result produced by the job.</returns>
    TResult Execute();
}

/// <summary>
/// Represents an asynchronous job executed by the job system.
/// </summary>
public interface IAsyncJob
{
    /// <summary>
    /// Gets the job name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the job asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <returns>A task that completes when the job finishes.</returns>
    Task ExecuteAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Represents an asynchronous job that produces a result.
/// </summary>
/// <typeparam name="TResult">Type of the produced result.</typeparam>
public interface IAsyncJob<TResult>
{
    /// <summary>
    /// Gets the job name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the job asynchronously and returns the result.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the execution.</param>
    /// <returns>A task that completes with the job result.</returns>
    Task<TResult> ExecuteAsync(CancellationToken cancellationToken);
}
