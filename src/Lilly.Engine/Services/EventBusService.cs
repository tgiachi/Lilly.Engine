using System.Collections;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Lilly.Engine.Core.Interfaces.Events;
using Lilly.Engine.Core.Interfaces.Services;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Lilly.Engine.Services;

public class EventBusService : IEventBusService, IDisposable
{
    private readonly ILogger _logger = Log.ForContext<EventBusService>();
    private readonly ConcurrentDictionary<Type, object> _listeners = new();
    private readonly Channel<EventDispatchJob> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;
    private readonly Subject<object> _allEventsSubject = new();

    private int _totalListenerCount;
    private bool _disposed;

    /// <summary>
    /// Observable that emits all events dispatched through the system.
    /// </summary>
    public IObservable<object> AllEventsObservable => _allEventsSubject;

    public EventBusService()
    {
        _channel = Channel.CreateUnbounded<EventDispatchJob>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            }
        );

        _processingTask = Task.Run(ProcessEventsAsync, _cts.Token);

        _logger.Information("EventBusService initialized with Channel");
    }

    /// <summary>
    /// Registers a listener for a specific event type.
    /// </summary>
    /// <returns>A subscription object that can be disposed to unsubscribe.</returns>
    public IDisposable Subscribe<TEvent>(IEventBusListener<TEvent> listener) where TEvent : class
    {
        var eventType = typeof(TEvent);
        var listeners = (ConcurrentBag<IEventBusListener<TEvent>>)_listeners.GetOrAdd(
            eventType,
            _ => new ConcurrentBag<IEventBusListener<TEvent>>()
        );

        listeners.Add(listener);
        Interlocked.Increment(ref _totalListenerCount);

        _logger.Verbose(
            "Registered listener {ListenerType} for event {EventType}. Total listeners: {TotalCount}",
            listener.GetType().Name,
            eventType.Name,
            _totalListenerCount
        );

        return new Subscription<TEvent>(this, listener);
    }

    /// <summary>
    /// Registers a function as a listener for a specific event type.
    /// </summary>
    /// <returns>A subscription object that can be disposed to unsubscribe.</returns>
    public IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class
    {
        var listener = new FunctionSignalListener<TEvent>(handler);

        // We need to keep a reference to the listener to be able to unsubscribe it later
        // A simple way is to wrap it in another object or use a tuple
        var subscription = Subscribe(listener);

        _logger.Verbose("Registered function handler for event {EventType}", typeof(TEvent).Name);

        // This is a bit of a hack to keep the original handler for unsubscription
        var disposable = new Subscription<TEvent>(this, new FunctionSignalListener<TEvent>(handler));

        return disposable;
    }

    /// <summary>
    /// Publishes an event to all registered listeners asynchronously via a channel.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventType = typeof(TEvent);

        _allEventsSubject.OnNext(eventData);

        if (!_listeners.TryGetValue(eventType, out var listenersObj))
        {
            _logger.Verbose("No listeners registered for event {EventType}", eventType.Name);

            return;
        }

        var listeners = (ConcurrentBag<IEventBusListener<TEvent>>)listenersObj;

        _logger.Verbose(
            "Publishing event {EventType} to {ListenerCount} listeners via channel",
            eventType.Name,
            listeners.Count
        );

        foreach (var listener in listeners)
        {
            var job = new EventDispatchJob<TEvent>(listener, eventData);
            await _channel.Writer.WriteAsync(job, cancellationToken);
        }
    }

    /// <summary>
    /// Dispatches an event to all registered listeners immediately on the calling thread.
    /// </summary>
    public async Task PublishImmediateAsync<TEvent>(TEvent eventData, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventType = typeof(TEvent);

        _allEventsSubject.OnNext(eventData);

        if (!_listeners.TryGetValue(eventType, out var listenersObj))
        {
            _logger.Verbose("No listeners registered for event {EventType}", eventType.Name);

            return;
        }

        var listeners = (ConcurrentBag<IEventBusListener<TEvent>>)listenersObj;

        _logger.Verbose(
            "Publishing event {EventType} to {ListenerCount} listeners immediately",
            eventType.Name,
            listeners.Count
        );

        var tasks = new List<Task>();

        foreach (var listener in listeners)
        {
            tasks.Add(listener.HandleAsync(eventData, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Returns total listener count.
    /// </summary>
    public int GetListenerCount()
        => _totalListenerCount;

    /// <summary>
    /// Returns listener count for a specific event type.
    /// </summary>
    public int GetListenerCount<TEvent>() where TEvent : class
    {
        if (_listeners.TryGetValue(typeof(TEvent), out var listenersObj))
        {
            var listeners = (ConcurrentBag<IEventBusListener<TEvent>>)listenersObj;

            return listeners.Count;
        }

        return 0;
    }

    /// <summary>
    /// Waits for all pending events in the channel to be processed.
    /// </summary>
    public async Task WaitForCompletionAsync()
    {
        _channel.Writer.Complete();
        await _processingTask;
    }

    private void Unsubscribe<TEvent>(IEventBusListener<TEvent> listener) where TEvent : class
    {
        var eventType = typeof(TEvent);

        if (_listeners.TryGetValue(eventType, out var listenersObj))
        {
            var listeners = (ConcurrentBag<IEventBusListener<TEvent>>)listenersObj;
            var updatedListeners = new ConcurrentBag<IEventBusListener<TEvent>>();

            // Re-add all listeners except the one we want to remove
            foreach (var l in listeners)
            {
                if (l != listener)
                {
                    updatedListeners.Add(l);
                }
            }

            _listeners.TryUpdate(eventType, updatedListeners, listeners);
            Interlocked.Decrement(ref _totalListenerCount);

            _logger.Verbose(
                "Unregistered listener {ListenerType} from event {EventType}. Total listeners: {TotalCount}",
                listener.GetType().Name,
                eventType.Name,
                _totalListenerCount
            );
        }
    }

    private void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : class
    {
        var eventType = typeof(TEvent);

        if (_listeners.TryGetValue(eventType, out var listenersObj))
        {
            var listeners = (ConcurrentBag<IEventBusListener<TEvent>>)listenersObj;
            var updatedListeners = new ConcurrentBag<IEventBusListener<TEvent>>();

            foreach (var l in listeners)
            {
                if (l is FunctionSignalListener<TEvent> functionListener && functionListener.HasSameHandler(handler))
                {
                    // Found the listener to remove, don't add it to the new bag
                    Interlocked.Decrement(ref _totalListenerCount);
                    _logger.Verbose(
                        "Unregistered function handler for event {EventType}. Total listeners: {TotalCount}",
                        eventType.Name,
                        _totalListenerCount
                    );
                }
                else
                {
                    updatedListeners.Add(l);
                }
            }

            _listeners.TryUpdate(eventType, updatedListeners, listeners);
        }
    }

    /// <summary>
    /// Background processor for event dispatch jobs.
    /// </summary>
    private async Task ProcessEventsAsync()
    {
        try
        {
            await foreach (var job in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    await job.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error while executing event dispatch job {JobType}", job.GetType().Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected on shutdown
            _logger.Information("Event processing task was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error in event processing task");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _cts.Cancel();
            _channel.Writer.TryComplete();
            _processingTask.Wait();
            _cts.Dispose();
        }

        _disposed = true;
    }

    private sealed class Subscription<TEvent> : IDisposable where TEvent : class
    {
        private readonly EventBusService _eventBus;
        private readonly IEventBusListener<TEvent> _listener;
        private Action _unsubscribeAction;

        public Subscription(EventBusService eventBus, IEventBusListener<TEvent> listener)
        {
            _eventBus = eventBus;
            _listener = listener;
            _unsubscribeAction = () => _eventBus.Unsubscribe(_listener);
        }

        public void Dispose()
        {
            _unsubscribeAction?.Invoke();
            _unsubscribeAction = null; // Prevent double un-subscription
        }
    }

    private sealed class FunctionSignalListener<TEvent> : IEventBusListener<TEvent> where TEvent : class
    {
        private readonly Func<TEvent, CancellationToken, Task> _handler;

        public FunctionSignalListener(Func<TEvent, CancellationToken, Task> handler)
        {
            _handler = handler;
        }

        public Task HandleAsync(TEvent evt, CancellationToken cancellationToken)
        {
            return _handler(evt, cancellationToken);
        }

        public bool HasSameHandler(Func<TEvent, CancellationToken, Task> handler)
        {
            return _handler == handler;
        }
    }

    private abstract class EventDispatchJob
    {
        public abstract Task ExecuteAsync();
    }

    private sealed class EventDispatchJob<TEvent> : EventDispatchJob where TEvent : class
    {
        private readonly IEventBusListener<TEvent> _listener;
        private readonly TEvent _eventData;

        public EventDispatchJob(IEventBusListener<TEvent> listener, TEvent eventData)
        {
            _listener = listener;
            _eventData = eventData;
        }

        public override Task ExecuteAsync()
        {
            return _listener.HandleAsync(_eventData, CancellationToken.None);
        }
    }

}
