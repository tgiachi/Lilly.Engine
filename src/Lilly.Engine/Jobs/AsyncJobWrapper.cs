using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Wraps an asynchronous job for execution.
/// </summary>
internal sealed class AsyncJobWrapper : QueuedJob
{
    private readonly IAsyncJob _job;
    private readonly TaskCompletionSource<bool>? _completionSource;

    public AsyncJobWrapper(IAsyncJob job, CancellationToken cancellationToken, bool captureCompletion)
        : base(job.Name, cancellationToken)
    {
        _job = job;
        _completionSource = captureCompletion
                                ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
                                : null;
    }

    public Task CompletionTask => _completionSource?.Task ?? Task.CompletedTask;

    protected override async Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _job.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            _completionSource?.TrySetResult(true);
        }
        catch (OperationCanceledException oce)
        {
            var token = oce.CancellationToken.CanBeCanceled ? oce.CancellationToken : cancellationToken;
            _completionSource?.TrySetCanceled(token);

            throw;
        }
        catch (Exception ex)
        {
            _completionSource?.TrySetException(ex);

            throw;
        }
    }
}
