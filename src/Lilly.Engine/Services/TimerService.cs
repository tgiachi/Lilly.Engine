using Lilly.Engine.Core.Data.Internal.Timers;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Rendering.Core.Context;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Implements the timer service for scheduling and managing timed operations using a Timer Wheel algorithm.
/// Provides O(1) insertion, removal, and tick advancement for high-performance timer management.
/// </summary>
public class TimerService : ITimerService
{
    private readonly ILogger _logger = Log.ForContext<TimerService>();

    private readonly ObjectPool<TimerDataObject> _timerDataPool = ObjectPool.Create(
        new DefaultPooledObjectPolicy<TimerDataObject>()
    );

    private readonly SemaphoreSlim _timerSemaphore = new(1, 1);

    private readonly TimerWheel _timerWheel;

    private readonly Dictionary<string, TimerDataObject> _timerById = new();
    private readonly Dictionary<string, string> _timerIdByName = new();

    private readonly RenderContext _context;

    /// <summary>
    /// Initializes a new instance of the TimerService class.
    /// </summary>
    /// <param name="context">The graphics renderer.</param>
    public TimerService(RenderContext context)
    {
        _context = context;
        _context.Renderer.OnUpdate += Update;

        // Initialize timer wheel with 512 slots and 16ms tick duration (~60fps)
        _timerWheel = new(512, 16.0);
    }

    /// <summary>
    /// Disposes the timer service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _context.Renderer.OnUpdate -= Update;
        _timerSemaphore.Dispose();
        _timerWheel.Clear();
        _timerById.Clear();
        _timerIdByName.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Registers a synchronous timer with the specified name, interval in milliseconds, callback, delay in milliseconds, and repeat
    /// option.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="intervalInMs">The interval in milliseconds.</param>
    /// <param name="callback">The callback action to execute.</param>
    /// <param name="delayInMs">The delay in milliseconds before first execution.</param>
    /// <param name="repeat">Whether the timer should repeat.</param>
    /// <returns>The unique ID of the registered timer.</returns>
    public string RegisterTimer(string name, double intervalInMs, Action callback, double delayInMs = 0, bool repeat = false)
    {
        _timerSemaphore.Wait();

        try
        {
            // Check if timer with this name already exists
            if (_timerIdByName.TryGetValue(name, out var existingTimerId))
            {
                _logger.Warning("Timer with name {Name} already exists. Unregistering it.", name);
                UnregisterTimerInternal(existingTimerId);
            }

            var timerId = Guid.NewGuid().ToString();
            var timer = _timerDataPool.Get();

            timer.Name = name;
            timer.Id = timerId;
            timer.IntervalInMs = intervalInMs;
            timer.Callback = callback;
            timer.Repeat = repeat;
            timer.RemainingTimeInMs = intervalInMs;
            timer.DelayInMs = delayInMs;
            timer.IsAsync = false;

            // Use delay if specified, otherwise use interval
            var initialDelay = delayInMs > 0 ? delayInMs : intervalInMs;

            // Add to timer wheel
            _timerWheel.AddTimer(timer, initialDelay);

            // Add to lookup dictionaries
            _timerById[timerId] = timer;
            _timerIdByName[name] = timerId;

            _logger.Debug(
                "Registered timer: {TimerId}, Interval: {IntervalInMs} ms, Delay: {DelayInMs} ms, Repeat: {Repeat}",
                timerId,
                intervalInMs,
                initialDelay,
                repeat
            );

            return timerId;
        }
        finally
        {
            _timerSemaphore.Release();
        }
    }

    /// <summary>
    /// Registers a synchronous timer with the specified name, interval as TimeSpan, callback, delay as TimeSpan, and repeat option.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="interval">The interval as TimeSpan.</param>
    /// <param name="callback">The callback action to execute.</param>
    /// <param name="delay">The delay as TimeSpan before first execution.</param>
    /// <param name="repeat">Whether the timer should repeat.</param>
    /// <returns>The unique ID of the registered timer.</returns>
    public string RegisterTimer(
        string name,
        TimeSpan interval,
        Action callback,
        TimeSpan delay = default,
        bool repeat = false
    )
        => RegisterTimer(name, interval.TotalMilliseconds, callback, delay.TotalMilliseconds, repeat);

    /// <summary>
    /// Registers an asynchronous timer with the specified name, interval in milliseconds, callback, delay in milliseconds, and
    /// repeat option.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="intervalInMs">The interval in milliseconds.</param>
    /// <param name="callback">The asynchronous callback function to execute.</param>
    /// <param name="delayInMs">The delay in milliseconds before first execution.</param>
    /// <param name="repeat">Whether the timer should repeat.</param>
    /// <returns>The unique ID of the registered timer.</returns>
    public string RegisterTimerAsync(
        string name,
        double intervalInMs,
        Func<Task> callback,
        double delayInMs = 0,
        bool repeat = false
    )
    {
        _timerSemaphore.Wait();

        try
        {
            // Check if timer with this name already exists
            if (_timerIdByName.TryGetValue(name, out var existingTimerId))
            {
                _logger.Warning("Timer with name {Name} already exists. Unregistering it.", name);
                UnregisterTimerInternal(existingTimerId);
            }

            var timerId = Guid.NewGuid().ToString();
            var timer = _timerDataPool.Get();

            timer.Name = name;
            timer.Id = timerId;
            timer.IntervalInMs = intervalInMs;
            timer.AsyncCallback = callback;
            timer.Repeat = repeat;
            timer.RemainingTimeInMs = intervalInMs;
            timer.DelayInMs = delayInMs;
            timer.IsAsync = true;

            // Use delay if specified, otherwise use interval
            var initialDelay = delayInMs > 0 ? delayInMs : intervalInMs;

            // Add to timer wheel
            _timerWheel.AddTimer(timer, initialDelay);

            // Add to lookup dictionaries
            _timerById[timerId] = timer;
            _timerIdByName[name] = timerId;

            _logger.Debug(
                "Registered async timer: {TimerId}, Interval: {IntervalInMs} ms, Delay: {DelayInMs} ms, Repeat: {Repeat}",
                timerId,
                intervalInMs,
                initialDelay,
                repeat
            );

            return timerId;
        }
        finally
        {
            _timerSemaphore.Release();
        }
    }

    /// <summary>
    /// Registers an asynchronous timer with the specified name, interval as TimeSpan, callback, delay as TimeSpan, and repeat
    /// option.
    /// </summary>
    /// <param name="name">The name of the timer.</param>
    /// <param name="interval">The interval as TimeSpan.</param>
    /// <param name="callback">The asynchronous callback function to execute.</param>
    /// <param name="delay">The delay as TimeSpan before first execution.</param>
    /// <param name="repeat">Whether the timer should repeat.</param>
    /// <returns>The unique ID of the registered timer.</returns>
    public string RegisterTimerAsync(
        string name,
        TimeSpan interval,
        Func<Task> callback,
        TimeSpan delay = default,
        bool repeat = false
    )
        => RegisterTimerAsync(name, interval.TotalMilliseconds, callback, delay.TotalMilliseconds, repeat);

    /// <summary>
    /// Unregisters all registered timers.
    /// </summary>
    public void UnregisterAllTimers()
    {
        _timerSemaphore.Wait();

        try
        {
            var timerIds = _timerById.Keys.ToList();

            foreach (var timerId in timerIds)
            {
                UnregisterTimerInternal(timerId);
            }

            _logger.Information("Unregistered all timers ({Count} total)", timerIds.Count);
        }
        finally
        {
            _timerSemaphore.Release();
        }
    }

    /// <summary>
    /// Unregisters the timer with the specified ID.
    /// </summary>
    /// <param name="timerId">The ID of the timer to unregister.</param>
    public void UnregisterTimer(string timerId)
    {
        _timerSemaphore.Wait();

        try
        {
            UnregisterTimerInternal(timerId);
        }
        finally
        {
            _timerSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates the timer service with the current game time.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public void Update(GameTime gameTime)
    {
        _timerSemaphore.Wait();

        try
        {
            var elapsedMs = gameTime.ElapsedGameTimeAsTimeSpan.TotalMilliseconds;

            // Advance the timer wheel and get expired timers (O(1) + O(k) where k = expired timers)
            var expiredTimers = _timerWheel.Tick(elapsedMs);

            foreach (var timer in expiredTimers)
            {
                var executionSucceeded = true;

                try
                {
                    executionSucceeded = TimerExecutorGuard(timer);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing timer callback for {TimerId}", timer.Id);
                    executionSucceeded = false;
                }

                // If execution failed and timer should die on exception, remove it completely
                if (!executionSucceeded && timer.DieOnException)
                {
                    _timerById.Remove(timer.Id);
                    _timerIdByName.Remove(timer.Name);
                    _timerDataPool.Return(timer);

                    _logger.Information("Timer {TimerId} removed due to exception", timer.Id);
                }
                else if (timer.Repeat)
                {
                    // Re-add repeating timer to the wheel
                    _timerWheel.AddTimer(timer, timer.IntervalInMs);
                }
                else
                {
                    // One-shot timer, remove completely
                    _timerById.Remove(timer.Id);
                    _timerIdByName.Remove(timer.Name);
                    _timerDataPool.Return(timer);

                    _logger.Debug("One-shot timer {TimerId} completed and removed", timer.Id);
                }
            }
        }
        finally
        {
            _timerSemaphore.Release();
        }
    }

    private bool TimerExecutorGuard(TimerDataObject timerDataObject)
    {
        try
        {
            if (timerDataObject.IsAsync)
            {
                // Execute async callback synchronously within the event loop
                timerDataObject.AsyncCallback?.Invoke().GetAwaiter().GetResult();
            }
            else
            {
                timerDataObject.Callback();
            }

            return true;
        }
        catch (Exception ex)
        {
            if (timerDataObject.DieOnException)
            {
                _logger.Error(ex, "Timer {TimerId} encountered an error and will be unregistered", timerDataObject.Id);
            }
            else
            {
                _logger.Warning(ex, "Timer {TimerId} encountered an error", timerDataObject.Id);
            }

            return false;
        }
    }

    /// <summary>
    /// Internal method to unregister a timer without acquiring the semaphore.
    /// Must be called while holding the semaphore lock.
    /// </summary>
    /// <param name="timerId">The ID of the timer to unregister.</param>
    private void UnregisterTimerInternal(string timerId)
    {
        if (!_timerById.TryGetValue(timerId, out var timer))
        {
            _logger.Warning("Timer with ID {TimerId} not found", timerId);

            return;
        }

        // Remove from timer wheel
        _timerWheel.RemoveTimer(timer);

        // Remove from dictionaries
        _timerById.Remove(timerId);
        _timerIdByName.Remove(timer.Name);

        _logger.Information("Unregistered timer: {TimerId} ({Name})", timer.Id, timer.Name);

        // Return to pool
        _timerDataPool.Return(timer);
    }
}
