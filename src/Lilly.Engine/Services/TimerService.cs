using System.Collections.Concurrent;
using Lilly.Engine.Core.Data.Internal.Timers;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Rendering.Core.Context;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace Lilly.Engine.Services;

/// <summary>
/// Implements the timer service for scheduling and managing timed operations.
/// </summary>
public class TimerService : ITimerService
{
    private readonly ILogger _logger = Log.ForContext<TimerService>();

    private readonly ObjectPool<TimerDataObject> _timerDataPool = ObjectPool.Create(
        new DefaultPooledObjectPolicy<TimerDataObject>()
    );

    private readonly SemaphoreSlim _timerSemaphore = new(1, 1);

    private readonly BlockingCollection<TimerDataObject> _timers = new();

    private readonly RenderContext _context;

    /// <summary>
    /// Initializes a new instance of the TimerService class.
    /// </summary>
    /// <param name="context">The graphics renderer.</param>
    public TimerService(RenderContext context)
    {
        _context = context;
        _context.Renderer.OnUpdate += Update;
    }

    /// <summary>
    /// Disposes the timer service and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _context.Renderer.OnUpdate -= Update;
        _timerSemaphore.Dispose();
        _timers.Dispose();

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
        var existingTimer = _timers.FirstOrDefault(t => t.Name == name);

        if (existingTimer != null)
        {
            _logger.Warning("Timer with name {Name} already exists. Unregistering it.", name);
            UnregisterTimer(existingTimer.Id);
        }

        _timerSemaphore.Wait();

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

        _timers.Add(timer);

        _timerSemaphore.Release();

        _logger.Debug(
            "Registering timer: {TimerId}, Interval: {IntervalInSeconds} ms, Repeat: {Repeat}",
            timerId,
            intervalInMs,
            repeat
        );

        return timerId;
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
        var existingTimer = _timers.FirstOrDefault(t => t.Name == name);

        if (existingTimer != null)
        {
            _logger.Warning("Timer with name {Name} already exists. Unregistering it.", name);
            UnregisterTimer(existingTimer.Id);
        }

        _timerSemaphore.Wait();

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

        _timers.Add(timer);

        _timerSemaphore.Release();

        _logger.Debug(
            "Registering async timer: {TimerId}, Interval: {IntervalInSeconds} ms, Repeat: {Repeat}",
            timerId,
            intervalInMs,
            repeat
        );

        return timerId;
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

        while (_timers.TryTake(out var timer))
        {
            _logger.Information("Unregistering timer: {TimerId}", timer.Id);
        }

        _timerSemaphore.Release();
    }

    /// <summary>
    /// Unregisters the timer with the specified ID.
    /// </summary>
    /// <param name="timerId">The ID of the timer to unregister.</param>
    public void UnregisterTimer(string timerId)
    {
        _timerSemaphore.Wait();

        var timer = _timers.FirstOrDefault(t => t.Id == timerId);

        if (timer != null)
        {
            _timers.TryTake(out timer);
            _logger.Information("Unregistering timer: {TimerId}", timer.Id);
            _timerDataPool.Return(timer);
        }
        else
        {
            _logger.Warning("Timer with ID {TimerId} not found", timerId);
        }

        _timerSemaphore.Release();
    }

    /// <summary>
    /// Updates the timer service with the current game time.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public void Update(GameTime gameTime)
    {
        _timerSemaphore.Wait();

        foreach (var timer in _timers)
        {
            timer.DecrementRemainingTime(gameTime.ElapsedGameTimeAsTimeSpan.TotalMilliseconds);

            if (timer.RemainingTimeInMs <= 0)
            {
                try
                {
                    TimerExecutorGuard(timer);

                    //  _eventLoopService.EnqueueAction($"timer-{timer.Id}", () => TimerExecutorGuard(timer));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing timer callback for {TimerId}", timer.Id);
                }

                if (timer.Repeat)
                {
                    timer.ResetRemainingTime();
                }
                else
                {
                    _timers.TryTake(out _);
                    _logger.Information("Unregistering timer: {TimerId}", timer.Id);
                }
            }
        }

        _timerSemaphore.Release();
    }

    private void TimerExecutorGuard(TimerDataObject timerDataObject)
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
        }
        catch (Exception ex)
        {
            if (timerDataObject.DieOnException)
            {
                _logger.Error(ex, "Timer {TimerId} encountered an error and will be unregistered", timerDataObject.Id);
                UnregisterTimer(timerDataObject.Id);
            }
            else
            {
                _logger.Warning(ex, "Timer {TimerId} encountered an error", timerDataObject.Id);
            }
        }
    }
}
