namespace Lilly.Engine.Core.Data.Dispatchers;

/// <summary>
/// Captures a snapshot of an action execution result for diagnostics.
/// </summary>
public readonly record struct ActionExecutionRecord(
    string Name,
    ActionExecutionStatus Status,
    double DurationMs,
    DateTime CompletedAtUtc
);

/// <summary>
/// Represents the terminal state of an action execution.
/// </summary>
public enum ActionExecutionStatus
{
    Succeeded,
    Failed
}
