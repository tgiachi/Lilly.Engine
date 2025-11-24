using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a queued job in the job system.
/// </summary>
public abstract class QueuedJob : IComparable<QueuedJob>, IEquatable<QueuedJob>, IDisposable
{
    private readonly CancellationToken _jobCancellationToken;
    private readonly CancellationTokenSource _jobCts = new();

    protected QueuedJob(string name, JobPriority priority, CancellationToken cancellationToken)
    {
        Id = Guid.NewGuid();
        Name = name;
        Priority = priority;
        _jobCancellationToken = cancellationToken;
    }

    public Guid Id { get; }
    public string Name { get; }
    public JobPriority Priority { get; }

    public Task ExecuteAsync(CancellationToken serviceToken)
        => !_jobCancellationToken.CanBeCanceled ? ExecuteCoreAsync(serviceToken) : ExecuteWithLinkedTokenAsync(serviceToken);

    protected abstract Task ExecuteCoreAsync(CancellationToken cancellationToken);

    public bool TryCancel()
    {
        if (_jobCts.IsCancellationRequested)
            return false;

        try
        {
            _jobCts.Cancel();

            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private async Task ExecuteWithLinkedTokenAsync(CancellationToken serviceToken)
    {
        using var linkedCts =
            CancellationTokenSource.CreateLinkedTokenSource(serviceToken, _jobCancellationToken, _jobCts.Token);
        await ExecuteCoreAsync(linkedCts.Token).ConfigureAwait(false);
    }

    public int CompareTo(QueuedJob? other)
    {
        if (other == null)
            return 1;

        // Higher priority comes first (reversed comparison)
        var priorityComparison = other.Priority.CompareTo(Priority);

        if (priorityComparison != 0)
            return priorityComparison;

        // Same priority - maintain FIFO order using ID (first in has lower GUID when ordered)
        return Id.CompareTo(other.Id);
    }

    public bool Equals(QueuedJob? other)
    {
        return other != null && Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return obj is QueuedJob other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(QueuedJob? left, QueuedJob? right)
    {
        if (left is null)
            return right is null;

        return left.Equals(right);
    }

    public static bool operator !=(QueuedJob? left, QueuedJob? right)
    {
        return !(left == right);
    }

    public static bool operator <(QueuedJob? left, QueuedJob? right)
    {
        return left is null ? right is not null : left.CompareTo(right) < 0;
    }

    public static bool operator <=(QueuedJob? left, QueuedJob? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    public static bool operator >(QueuedJob? left, QueuedJob? right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    public static bool operator >=(QueuedJob? left, QueuedJob? right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }

    public void Dispose()
    {
        _jobCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
