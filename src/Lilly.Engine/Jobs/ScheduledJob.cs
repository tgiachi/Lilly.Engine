using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Jobs;

/// <summary>
/// Represents a job scheduled for execution in the job system.
/// </summary>
internal sealed record ScheduledJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required JobPriority Priority { get; init; }
    public Action? OnComplete { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public Func<CancellationToken, Task>? ExecuteAction { get; set; }

    private int _cancelled;

    public bool IsCancelled => Interlocked.CompareExchange(ref _cancelled, 0, 0) == 1;

    public bool TryCancel() => Interlocked.CompareExchange(ref _cancelled, 1, 0) == 0;
}
