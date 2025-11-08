using Lilly.Engine.Core.Data.Privimitives;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>Provides methods for registering, unregistering, and managing timers.</summary>
public interface ITimerService : IDisposable
{
    /// <summary>
    /// Registers a synchronous timer with the specified name, interval in milliseconds, callback, delay in milliseconds,
    /// and repeat option.
    /// </summary>
    string RegisterTimer(string name, double intervalInMs, Action callback, double delayInMs = 0, bool repeat = false);

    /// <summary>
    /// Registers a synchronous timer with the specified name, interval as TimeSpan, callback, delay as TimeSpan, and
    /// repeat option.
    /// </summary>
    string RegisterTimer(string name, TimeSpan interval, Action callback, TimeSpan delay = default, bool repeat = false);

    /// <summary>
    /// Registers an asynchronous timer with the specified name, interval in milliseconds, callback, delay in milliseconds,
    /// and repeat option.
    /// </summary>
    string RegisterTimerAsync(
        string name,
        double intervalInMs,
        Func<Task> callback,
        double delayInMs = 0,
        bool repeat = false
    );

    /// <summary>
    /// Registers an asynchronous timer with the specified name, interval as TimeSpan, callback, delay as TimeSpan, and
    /// repeat option.
    /// </summary>
    string RegisterTimerAsync(
        string name,
        TimeSpan interval,
        Func<Task> callback,
        TimeSpan delay = default,
        bool repeat = false
    );

    /// <summary>Unregisters all registered timers.</summary>
    void UnregisterAllTimers();

    /// <summary>Unregisters the timer with the specified ID.</summary>
    void UnregisterTimer(string timerId);

    /// <summary>
    /// Updates the timer service with the current game time.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    void Update(GameTime gameTime);
}
