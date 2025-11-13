using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a synchronous job that returns a result.
/// </summary>
internal sealed class SynchronousResultJob<TResult> : QueuedJob
{
    private readonly IJob<TResult> _job;
    private readonly TaskCompletionSource<TResult> _completionSource;

    public SynchronousResultJob(IJob<TResult> job, CancellationToken cancellationToken)
        : base(job.Name, cancellationToken)
    {
        _job = job;
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task<TResult> CompletionTask => _completionSource.Task;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = _job.Execute();
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

        return Task.CompletedTask;
    }
}