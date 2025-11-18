using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a synchronous fire-and-forget job.
/// </summary>
internal sealed class SynchronousFireAndForgetJob : QueuedJob
{
    private readonly IJob _job;

    public SynchronousFireAndForgetJob(IJob job, JobPriority priority)
        : base(job.Name, priority, CancellationToken.None)
        => _job = job;

    protected override Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _job.Execute();

        return Task.CompletedTask;
    }
}
