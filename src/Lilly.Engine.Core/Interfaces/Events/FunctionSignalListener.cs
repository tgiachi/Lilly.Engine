using Lilly.Engine.Core.Interfaces.Services;
using Serilog;

namespace Lilly.Engine.Core.Interfaces.Events;

/// <summary>
/// Adapter class that wraps a function to implement IEventBusListener
/// </summary>
public class FunctionSignalListener<TEvent> : IEventBusListener<TEvent>
    where TEvent : class
{
    private readonly Func<TEvent, Task> _handler;

    /// <summary>
    /// Initializes a new instance of the FunctionSignalListener class.
    /// </summary>
    /// <param name="handler">The function to handle the event.</param>
    public FunctionSignalListener(Func<TEvent, Task> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    /// <summary>
    /// Handles the event asynchronously.
    /// </summary>
    /// <param name="signalEvent">The event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task HandleAsync(TEvent signalEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            return _handler(signalEvent);
        }
        catch (Exception ex)
        {
            Log.Logger.ForContext(GetType()).Error(ex, ex.Message);
            throw new InvalidOperationException(
                $"Error executing handler for event {typeof(TEvent).Name}",
                ex
            );
        }
    }

    /// <summary>
    /// Checks if this wrapper contains the same handler function
    /// </summary>
    public bool HasSameHandler(Func<TEvent, Task> handler)
    {
        return _handler.Equals(handler);
    }
}
