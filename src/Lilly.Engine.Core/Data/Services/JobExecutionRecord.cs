using Lilly.Engine.Core.Interfaces.Jobs;

namespace Lilly.Engine.Core.Data.Services;

/// <summary>
/// Captures a snapshot of a job execution result for diagnostics.
/// </summary>
public readonly record struct JobExecutionRecord(
    string Name,
    JobPriority Priority,
    JobExecutionStatus Status,
    double DurationMs,
    DateTime CompletedAtUtc,
    int ThreadId
);

/// <summary>
/// Represents the terminal state of a job execution.
/// </summary>
public enum JobExecutionStatus
{
    Succeeded,
    Cancelled,
    Failed
}
