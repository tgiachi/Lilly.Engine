using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Serilog;

namespace Lilly.Engine.Services.Input;

/// <summary>
/// Handles input focus management and focus stack operations.
/// </summary>
internal class FocusManager
{
    private readonly ILogger _logger = Log.ForContext<FocusManager>();
    private readonly Stack<IInputReceiver> _focusStack = new();

    /// <summary>
    /// Gets the current input focus receiver.
    /// </summary>
    public IInputReceiver? CurrentFocus { get; private set; }

    /// <summary>
    /// Gets the depth of the focus stack.
    /// </summary>
    public int FocusStackDepth => _focusStack.Count;

    /// <summary>
    /// Clears the current input focus.
    /// </summary>
    public void ClearFocus()
    {
        SetFocus(null);
    }

    /// <summary>
    /// Disposes of focus-related resources.
    /// </summary>
    public void Dispose()
    {
        _focusStack.Clear();
        CurrentFocus = null;
    }

    /// <summary>
    /// Pops the top focus receiver from the stack.
    /// </summary>
    public void PopFocusStack()
    {
        if (CurrentFocus != null)
        {
            CurrentFocus.HasFocus = false;
        }

        if (_focusStack.TryPop(out var previous))
        {
            CurrentFocus = previous;
            CurrentFocus.HasFocus = true;
            _logger.Debug(
                "Popped focus stack, restored {Receiver} (depth: {Depth})",
                CurrentFocus.GetType().Name,
                _focusStack.Count
            );
        }
        else
        {
            CurrentFocus = null;
            _logger.Debug("Popped focus stack, no previous focus");
        }
    }

    /// <summary>
    /// Pushes a new focus receiver onto the focus stack.
    /// </summary>
    /// <param name="receiver">The receiver to push.</param>
    public void PushFocusStack(IInputReceiver receiver)
    {
        ArgumentNullException.ThrowIfNull(receiver);

        if (CurrentFocus != null)
        {
            _focusStack.Push(CurrentFocus);
            CurrentFocus.HasFocus = false;
        }

        CurrentFocus = receiver;
        CurrentFocus.HasFocus = true;

        _logger.Debug("Pushed {Receiver} to focus stack (depth: {Depth})", receiver.GetType().Name, _focusStack.Count + 1);
    }

    /// <summary>
    /// Sets the input focus to a specific receiver.
    /// </summary>
    /// <param name="receiver">The receiver to give focus to.</param>
    public void SetFocus(IInputReceiver? receiver)
    {
        if (CurrentFocus != null)
        {
            CurrentFocus.HasFocus = false;
        }

        CurrentFocus = receiver;

        if (CurrentFocus != null)
        {
            CurrentFocus.HasFocus = true;
            _logger.Debug("Input focus set to {Receiver}", CurrentFocus.Name ?? CurrentFocus.GetType().Name);
        }
        else
        {
            _logger.Debug("Input focus cleared");
        }
    }
}
