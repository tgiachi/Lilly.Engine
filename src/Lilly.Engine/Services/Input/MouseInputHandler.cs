using Serilog;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.Services.Input;

/// <summary>
/// Handles mouse input states, listeners, and click/movement logic.
/// </summary>
internal sealed class MouseInputHandler
{
    private readonly ILogger _logger = Log.ForContext<MouseInputHandler>();
    private readonly List<Action<Vector2D<int>, Vector2D<int>>> _globalMouseMovementListeners = [];
    private readonly Dictionary<string, List<Action<Vector2D<int>, Vector2D<int>>>> _mouseMovementListeners = new();
    private readonly List<Action<MouseButton, Vector2D<int>>> _globalMouseClickListeners = [];
    private readonly Dictionary<string, List<Action<MouseButton, Vector2D<int>>>> _mouseClickListeners = new();
    private readonly HashSet<MouseButton> _processedMouseClicks = [];

    private readonly IMouse _mouse;
    private Vector2D<int> _lastMousePosition = Vector2D<int>.Zero;

    public MouseInputHandler(IMouse mouse)
        => _mouse = mouse;

    /// <summary>
    /// Gets the current mouse snapshot.
    /// </summary>
    public MouseState CurrentMouseState { get; private set; }

    /// <summary>
    /// Gets the previous mouse snapshot.
    /// </summary>
    public MouseState PreviousMouseState { get; private set; }

    /// <summary>
    /// Binds a callback to mouse click events globally (always active, regardless of context).
    /// </summary>
    /// <param name="callback">The callback to execute when a mouse button is clicked.</param>
    public void BindMouseClick(Action<MouseButton, Vector2D<int>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!_globalMouseClickListeners.Contains(callback))
        {
            _globalMouseClickListeners.Add(callback);
            _logger.Debug("Bound global mouse click callback");
        }
    }

    /// <summary>
    /// Binds a callback to mouse click events, only active in a specific context.
    /// </summary>
    /// <param name="callback">The callback to execute when a mouse button is clicked.</param>
    /// <param name="context">The context in which this callback is active.</param>
    public void BindMouseClick(Action<MouseButton, Vector2D<int>> callback, string context)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentException.ThrowIfNullOrEmpty(context);

        if (!_mouseClickListeners.TryGetValue(context, out var listeners))
        {
            listeners = new();
            _mouseClickListeners[context] = listeners;
        }

        if (!listeners.Contains(callback))
        {
            listeners.Add(callback);
            _logger.Debug("Bound mouse click callback to context {Context}", context);
        }
    }

    /// <summary>
    /// Binds a callback to mouse movement globally (always active, regardless of context).
    /// </summary>
    /// <param name="callback">The callback to execute every frame mouse moves.</param>
    public void BindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (!_globalMouseMovementListeners.Contains(callback))
        {
            _globalMouseMovementListeners.Add(callback);
            _logger.Debug("Bound global mouse movement callback");
        }
    }

    /// <summary>
    /// Binds a callback to mouse movement, only active in a specific context.
    /// </summary>
    /// <param name="callback">The callback to execute every frame mouse moves.</param>
    /// <param name="context">The context in which this callback is active.</param>
    public void BindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback, string context)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentException.ThrowIfNullOrEmpty(context);

        if (!_mouseMovementListeners.TryGetValue(context, out var listeners))
        {
            listeners = new();
            _mouseMovementListeners[context] = listeners;
        }

        if (!listeners.Contains(callback))
        {
            listeners.Add(callback);
            _logger.Debug("Bound mouse movement callback to context {Context}", context);
        }
    }

    /// <summary>
    /// Disposes of mouse-related resources.
    /// </summary>
    public void Dispose()
    {
        _globalMouseMovementListeners.Clear();
        _mouseMovementListeners.Clear();
        _globalMouseClickListeners.Clear();
        _mouseClickListeners.Clear();
        _processedMouseClicks.Clear();
    }

    /// <summary>
    /// Gets the mouse delta (movement since last frame).
    /// </summary>
    /// <returns>The mouse delta as a Vector2D with X and Y components.</returns>
    public Vector2D<int> GetMouseDelta()
    {
        var currentPos = new Vector2D<int>((int)CurrentMouseState.Position.X, (int)CurrentMouseState.Position.Y);
        var delta = currentPos - _lastMousePosition;
        _lastMousePosition = currentPos;

        return delta;
    }

    /// <summary>
    /// Checks if a mouse button is currently down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down.</returns>
    public bool IsMouseButtonDown(MouseButton button)
    {
        var silkButton = button switch
        {
            MouseButton.Left     => Silk.NET.Input.MouseButton.Left,
            MouseButton.Right    => Silk.NET.Input.MouseButton.Right,
            MouseButton.Middle   => Silk.NET.Input.MouseButton.Middle,
            MouseButton.XButton1 => Silk.NET.Input.MouseButton.Button4,
            MouseButton.XButton2 => Silk.NET.Input.MouseButton.Button5,
            _                    => Silk.NET.Input.MouseButton.Unknown
        };

        return CurrentMouseState.IsButtonPressed(silkButton);
    }

    /// <summary>
    /// Checks if a mouse button was just pressed this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just pressed.</returns>
    public bool IsMouseButtonPressed(MouseButton button)
    {
        if (PreviousMouseState == null)
        {
            return false;
        }

        var silkButton = button switch
        {
            MouseButton.Left     => Silk.NET.Input.MouseButton.Left,
            MouseButton.Right    => Silk.NET.Input.MouseButton.Right,
            MouseButton.Middle   => Silk.NET.Input.MouseButton.Middle,
            MouseButton.XButton1 => Silk.NET.Input.MouseButton.Button4,
            MouseButton.XButton2 => Silk.NET.Input.MouseButton.Button5,
            _                    => Silk.NET.Input.MouseButton.Unknown
        };

        return !PreviousMouseState.IsButtonPressed(silkButton) && CurrentMouseState.IsButtonPressed(silkButton);
    }

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just released.</returns>
    public bool IsMouseButtonReleased(MouseButton button)
    {
        var silkButton = button switch
        {
            MouseButton.Left     => Silk.NET.Input.MouseButton.Left,
            MouseButton.Right    => Silk.NET.Input.MouseButton.Right,
            MouseButton.Middle   => Silk.NET.Input.MouseButton.Middle,
            MouseButton.XButton1 => Silk.NET.Input.MouseButton.Button4,
            MouseButton.XButton2 => Silk.NET.Input.MouseButton.Button5,
            _                    => Silk.NET.Input.MouseButton.Unknown
        };

        return PreviousMouseState.IsButtonPressed(silkButton) && !CurrentMouseState.IsButtonPressed(silkButton);
    }

    /// <summary>
    /// Unbinds a global mouse click callback.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    public void UnbindMouseClick(Action<MouseButton, Vector2D<int>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_globalMouseClickListeners.Remove(callback))
        {
            _logger.Debug("Unbound global mouse click callback");
        }
    }

    /// <summary>
    /// Unbinds a mouse click callback for a specific context.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    /// <param name="context">The context from which to remove the binding.</param>
    public void UnbindMouseClick(Action<MouseButton, Vector2D<int>> callback, string context)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentException.ThrowIfNullOrEmpty(context);

        if (_mouseClickListeners.TryGetValue(context, out var listeners))
        {
            if (listeners.Remove(callback))
            {
                _logger.Debug("Unbound mouse click callback from context {Context}", context);
            }

            if (listeners.Count == 0)
            {
                _mouseClickListeners.Remove(context);
            }
        }
    }

    /// <summary>
    /// Unbinds a global mouse movement callback.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    public void UnbindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (_globalMouseMovementListeners.Remove(callback))
        {
            _logger.Debug("Unbound global mouse movement callback");
        }
    }

    /// <summary>
    /// Unbinds a mouse movement callback for a specific context.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    /// <param name="context">The context from which to remove the binding.</param>
    public void UnbindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback, string context)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentException.ThrowIfNullOrEmpty(context);

        if (_mouseMovementListeners.TryGetValue(context, out var listeners))
        {
            if (listeners.Remove(callback))
            {
                _logger.Debug("Unbound mouse movement callback from context {Context}", context);
            }

            if (listeners.Count == 0)
            {
                _mouseMovementListeners.Remove(context);
            }
        }
    }

    /// <summary>
    /// Updates the mouse state and processes listeners.
    /// </summary>
    /// <param name="currentContext">The current input context.</param>
    public void Update(string currentContext)
    {
        PreviousMouseState = CurrentMouseState;
        CurrentMouseState = _mouse.CaptureState();

        ProcessMouseMovement(currentContext);
        ProcessMouseClick(currentContext);
    }

    /// <summary>
    /// Processes mouse click listeners (both global and context-specific).
    /// Detects clicks on all mouse buttons and passes button and position to callbacks.
    /// </summary>
    /// <param name="currentContext">The current input context.</param>
    private void ProcessMouseClick(string currentContext)
    {
        _processedMouseClicks.Clear();
        var mousePosition = new Vector2D<int>((int)CurrentMouseState.Position.X, (int)CurrentMouseState.Position.Y);

        // Check all possible mouse buttons
        var buttons = new[]
            { MouseButton.Left, MouseButton.Right, MouseButton.Middle, MouseButton.XButton1, MouseButton.XButton2 };

        foreach (var button in buttons)
        {
            if (!IsMouseButtonPressed(button) || !_processedMouseClicks.Add(button))
            {
                continue;
            }

            // Process global listeners (always active)
            foreach (var listener in _globalMouseClickListeners)
            {
                try
                {
                    listener?.Invoke(button, mousePosition);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing global mouse click callback");
                }
            }

            // Process context-specific listeners (only if context matches)
            if (!string.IsNullOrEmpty(currentContext) &&
                _mouseClickListeners.TryGetValue(currentContext, out var listeners))
            {
                foreach (var listener in listeners)
                {
                    try
                    {
                        listener?.Invoke(button, mousePosition);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error executing context-specific mouse click callback");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Processes mouse movement listeners (both global and context-specific).
    /// Passes both delta and absolute position to callbacks.
    /// </summary>
    /// <param name="currentContext">The current input context.</param>
    private void ProcessMouseMovement(string currentContext)
    {
        var mouseDelta = GetMouseDelta();
        var mousePosition = new Vector2D<int>((int)CurrentMouseState.Position.X, (int)CurrentMouseState.Position.Y);

        var hasMoved = mouseDelta.X != 0 || mouseDelta.Y != 0;

        if (!hasMoved)
        {
            return;
        }

        // Process global listeners (always active)
        foreach (var listener in _globalMouseMovementListeners)
        {
            try
            {
                listener?.Invoke(mouseDelta, mousePosition);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error executing global mouse movement callback");
            }
        }

        // Process context-specific listeners (only if context matches)
        if (!string.IsNullOrEmpty(currentContext) &&
            _mouseMovementListeners.TryGetValue(currentContext, out var listeners))
        {
            foreach (var listener in listeners)
            {
                try
                {
                    listener?.Invoke(mouseDelta, mousePosition);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing context-specific mouse movement callback");
                }
            }
        }
    }
}
