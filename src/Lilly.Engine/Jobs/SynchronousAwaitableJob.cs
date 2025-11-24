using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a synchronous awaitable job.
/// </summary>
internal sealed class SynchronousAwaitableJob : QueuedJob
{
    private readonly IJob _job;
    private readonly TaskCompletionSource<bool> _completionSource;

    public SynchronousAwaitableJob(IJob job, JobPriority priority, CancellationToken cancellationToken)
        : base(job.Name, priority, cancellationToken)
    {
        _job = job;
        _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task CompletionTask => _completionSource.Task;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _job.Execute();
            _completionSource.TrySetResult(true);
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
