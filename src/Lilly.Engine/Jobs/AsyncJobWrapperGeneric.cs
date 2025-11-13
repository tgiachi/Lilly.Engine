using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Wraps a generic asynchronous job for execution.
/// </summary>
internal sealed class AsyncJobWrapper<TResult> : QueuedJob
{
    private readonly IAsyncJob<TResult> _job;
    private readonly TaskCompletionSource<TResult> _completionSource;

    public AsyncJobWrapper(IAsyncJob<TResult> job, CancellationToken cancellationToken)
        : base(job.Name, cancellationToken)
    {
        _job = job;
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task<TResult> CompletionTask => _completionSource.Task;

    protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
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