namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a queued job in the job system.
/// </summary>
internal abstract class QueuedJob
{
    private readonly CancellationToken _jobCancellationToken;

    protected QueuedJob(string name, CancellationToken cancellationToken)
    {
        Name = name;
        _jobCancellationToken = cancellationToken;
    }

    public string Name { get; }

    public Task ExecuteAsync(CancellationToken serviceToken)
    {
        return !_jobCancellationToken.CanBeCanceled ? ExecuteCoreAsync(serviceToken) : ExecuteWithLinkedTokenAsync(serviceToken);
    }

    protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken);

    private async Task ExecuteWithLinkedTokenAsync(CancellationToken serviceToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serviceToken, _jobCancellationToken);
        await ExecuteCoreAsync(linkedCts.Token).ConfigureAwait(false);
    }
}