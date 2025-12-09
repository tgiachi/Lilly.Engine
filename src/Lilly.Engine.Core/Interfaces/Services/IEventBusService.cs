using Lilly.Engine.Core.Interfaces.Events;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Provides an event bus for publishing and subscribing to events.
/// </summary>
public interface IEventBusService
{
    /// <summary>
    /// Observable that emits all events dispatched through the system.
    /// </summary>
    IObservable<object> AllEventsObservable { get; }

    /// <summary>
    /// Gets the current number of registered listeners for all event types.
    /// </summary>
    /// <returns>The total number of registered listeners.</returns>
    int GetListenerCount();

    /// <summary>
    /// Gets the current number of registered listeners for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <returns>The number of registered listeners for the specified event type.</returns>
    int GetListenerCount<TEvent>() where TEvent : class;

    /// <summary>
    /// Dispatches an event to all registered listeners asynchronously via a channel.
    /// This is the preferred method for most events.
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns></returns>
    Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Dispatches an event to all registered listeners immediately on the calling thread.
    /// Handlers are executed asynchronously.
    /// </summary>
    /// <param name="eventData"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns>A task that completes when all handlers have been invoked.</returns>
    Task PublishImmediateAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Registers a listener for a specific event type.
    /// </summary>
    /// <param name="listener"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns>A subscription object that can be disposed to unsubscribe.</returns>
    IDisposable Subscribe<TEvent>(IEventBusListener<TEvent> listener)
        where TEvent : class;

    /// <summary>
    /// Registers a listener for a specific event type.
    /// </summary>
    /// <param name="handler"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <returns>A subscription object that can be disposed to unsubscribe.</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : class;

    /// <summary>
    /// Waits for all dispatched events in the channel to be processed.
    /// </summary>
    /// <returns></returns>
    Task WaitForCompletionAsync();
}
