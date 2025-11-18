using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Wraps a task-returning function that produces a result for execution in the job system.
/// </summary>
/// <typeparam name="TResult">Type of the result produced by the task.</typeparam>
public sealed class TaskResultJob<TResult> : QueuedJob
{
    private readonly Func<CancellationToken, Task<TResult>> _taskFactory;
    private readonly TaskCompletionSource<TResult> _completionSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskResultJob{TResult}"/> class with a task factory.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="taskFactory">Function that creates and returns the task to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the job.</param>
    public TaskResultJob(
        string name,
        Func<CancellationToken, Task<TResult>> taskFactory,
        JobPriority priority,
        CancellationToken cancellationToken
    )
        : base(name, priority, cancellationToken)
    {
        _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskResultJob{TResult}"/> class with a parameterless task factory.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="taskFactory">Function that creates and returns the task to execute.</param>
    /// <param name="priority">The execution priority of the job.</param>
    /// <param name="cancellationToken">Token used to cancel the job.</param>
    public TaskResultJob(string name, Func<Task<TResult>> taskFactory, JobPriority priority, CancellationToken cancellationToken)
        : base(name, priority, cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(taskFactory);

        _taskFactory = _ => taskFactory();
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Gets a task that completes when the job finishes and returns the result.
    /// </summary>
    public Task<TResult> CompletionTask => _completionSource.Task;

    protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _taskFactory(cancellationToken).ConfigureAwait(false);
            _completionSource.TrySetResult(result);
        }
        catch (OperationCanceledException oce)
        {
            var token = oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : cancellationToken;
            _completionSource.TrySetCanceled(token);

            throw;
        }
        catch (Exception ex)
        {
            _completionSource.TrySetException(ex);

            throw;
        }
    }
}
