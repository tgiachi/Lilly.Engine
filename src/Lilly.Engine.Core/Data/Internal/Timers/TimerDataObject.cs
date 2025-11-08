namespace Lilly.Engine.Core.Data.Internal.Timers;

/// <summary>
/// Internal data object representing a timer instance
/// </summary>
public class TimerDataObject
{
    /// <summary>
    /// Unique identifier for the timer
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the timer
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Interval between timer executions in milliseconds
    /// </summary>
    public double IntervalInMs { get; set; }

    /// <summary>
    /// Initial delay before first execution in milliseconds
    /// </summary>
    public double DelayInMs { get; set; }

    /// <summary>
    /// Remaining time until next execution in milliseconds
    /// </summary>
    public double RemainingTimeInMs { get; set; }

    /// <summary>
    /// Whether the timer should repeat after firing
    /// </summary>
    public bool Repeat { get; set; }

    /// <summary>
    /// Whether the timer should be unregistered if an exception occurs
    /// </summary>
    public bool DieOnException { get; set; } = true;

    /// <summary>
    /// Callback action to execute when timer fires
    /// </summary>
    public Action Callback { get; set; } = () => { };

    /// <summary>
    /// Async callback task to execute when timer fires
    /// </summary>
    public Func<Task>? AsyncCallback { get; set; }

    /// <summary>
    /// Whether this timer uses async callback
    /// </summary>
    public bool IsAsync { get; set; }

    /// <summary>
    /// Whether this timer is currently active (not yet fired for one-shot timers)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Decrements the remaining time by the specified amount
    /// </summary>
    /// <param name="deltaTimeMs">Time to subtract in milliseconds</param>
    public void DecrementRemainingTime(double deltaTimeMs)
    {
        RemainingTimeInMs -= deltaTimeMs;
    }

    /// <summary>
    /// Resets the timer data for reuse in object pool
    /// </summary>
    public void Reset()
    {
        Id = string.Empty;
        Name = string.Empty;
        IntervalInMs = 0;
        DelayInMs = 0;
        RemainingTimeInMs = 0;
        Repeat = false;
        DieOnException = true;
        Callback = () => { };
        AsyncCallback = null;
        IsAsync = false;
        IsActive = true;
    }

    /// <summary>
    /// Resets the remaining time to the interval value
    /// </summary>
    public void ResetRemainingTime()
    {
        RemainingTimeInMs = IntervalInMs;
    }
}
