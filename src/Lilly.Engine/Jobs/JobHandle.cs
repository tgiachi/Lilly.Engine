using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a handle to a scheduled job without a result.
/// </summary>
internal sealed class JobHandle : IJobHandle
{
    private readonly ScheduledJob _job;
    private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public JobHandle(ScheduledJob job) => _job = job;

    public Task CompletionTask => _tcs.Task;
    public Guid Id => _job.Id;
    public string JobName => _job.Name;
    public JobPriority Priority => _job.Priority;
    public bool IsCompleted => _tcs.Task.IsCompleted;

    public bool Cancel() => _job.TryCancel();

    internal void SetResult() => _tcs.TrySetResult(true);
    internal void SetCanceled() => _tcs.TrySetCanceled();
    internal void SetException(Exception ex) => _tcs.TrySetException(ex);
}

/// <summary>
/// Represents a handle to a scheduled job with a result.
/// </summary>
/// <typeparam name="TResult">The type of result produced by the job.</typeparam>
internal sealed class JobHandle<TResult> : IJobHandle<TResult>
{
    private readonly ScheduledJob _job;
    private readonly TaskCompletionSource<TResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public JobHandle(ScheduledJob job) => _job = job;

    Task IJobHandle.CompletionTask => _tcs.Task.ContinueWith(object? (_) => null);
    public Task<TResult> CompletionTask => _tcs.Task;
    public Guid Id => _job.Id;
    public string JobName => _job.Name;
    public JobPriority Priority => _job.Priority;
    public bool IsCompleted => _tcs.Task.IsCompleted;

    public bool Cancel() => _job.TryCancel();

    internal void SetResult(TResult result) => _tcs.TrySetResult(result);
    internal void SetCanceled() => _tcs.TrySetCanceled();
    internal void SetException(Exception ex) => _tcs.TrySetException(ex);
}
