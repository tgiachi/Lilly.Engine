using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a handle to a scheduled job without a result.
/// </summary>
internal sealed class JobHandle : IJobHandle
{
    private readonly QueuedJob _queuedJob;
    private readonly TaskCompletionSource<bool> _completionSource;

    public JobHandle(QueuedJob queuedJob)
    {
        _queuedJob = queuedJob;
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Link the underlying job's completion to this handle
        LinkCompletion();
    }

    public Task CompletionTask => _completionSource.Task;
    public Guid Id => _queuedJob.Id;
    public string JobName => _queuedJob.Name;
    public JobPriority Priority => _queuedJob.Priority;
    public bool IsCompleted => _completionSource.Task.IsCompleted;

    public bool Cancel()
    {
        return _queuedJob.TryCancel();
    }

    internal void SetResult()
    {
        _completionSource.TrySetResult(true);
    }

    internal void SetCanceled(CancellationToken token)
    {
        _completionSource.TrySetCanceled(token);
    }

    internal void SetException(Exception ex)
    {
        _completionSource.TrySetException(ex);
    }

    private void LinkCompletion()
    {
        // This will be called from the job system after execution
        // The job system will call SetResult, SetCanceled, or SetException
    }
}

/// <summary>
/// Represents a handle to a scheduled job with a result.
/// </summary>
/// <typeparam name="TResult">The type of result produced by the job.</typeparam>
internal sealed class JobHandle<TResult> : IJobHandle<TResult>
{
    private readonly QueuedJob _queuedJob;
    private readonly TaskCompletionSource<TResult> _completionSource;

    public JobHandle(QueuedJob queuedJob)
    {
        _queuedJob = queuedJob;
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    Task IJobHandle.CompletionTask => _completionSource.Task.ContinueWith(_ => (object?)null)!;
    public Task<TResult> CompletionTask => _completionSource.Task;
    public Guid Id => _queuedJob.Id;
    public string JobName => _queuedJob.Name;
    public JobPriority Priority => _queuedJob.Priority;
    public bool IsCompleted => _completionSource.Task.IsCompleted;

    public bool Cancel()
    {
        return _queuedJob.TryCancel();
    }

    internal void SetResult(TResult result)
    {
        _completionSource.TrySetResult(result);
    }

    internal void SetCanceled(CancellationToken token)
    {
        _completionSource.TrySetCanceled(token);
    }

    internal void SetException(Exception ex)
    {
        _completionSource.TrySetException(ex);
    }
}
