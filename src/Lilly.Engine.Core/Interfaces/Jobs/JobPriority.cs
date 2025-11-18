namespace Lilly.Engine.Core.Interfaces.Jobs;

/// <summary>
/// Defines job execution priority levels.
/// </summary>
public enum JobPriority
{
    /// <summary>Low priority - executed after normal and high priority jobs.</summary>
    Low = 0,

    /// <summary>Normal priority - default execution level.</summary>
    Normal = 1,

    /// <summary>High priority - executed before normal and low priority jobs.</summary>
    High = 2
}
