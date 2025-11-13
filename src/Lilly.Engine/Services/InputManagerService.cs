using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Events;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Data.Input;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using ButtonState = Lilly.Engine.Rendering.Core.Data.Input.ButtonState;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.Services;

/// <summary>
/// Implements the input management service using Silk.NET Input.
/// Handles keyboard and mouse input, key bindings, and input focus management.
/// </summary>
public class InputManagerService : IInputManagerService
{
    private readonly ILogger _logger = Log.ForContext<InputManagerService>();
    private readonly Stack<IInputReceiver> _focusStack = new();
    private readonly Dictionary<KeyBinding, (Action Action, string? Context)> _keyBindings = new();
    private readonly HashSet<KeyBinding> _processedBindings = [];
    private readonly Dictionary<Key, float> _keyPressDuration = new();
    private readonly Dictionary<Key, float> _keyRepeatTimers = new();
    private readonly List<Action<Vector2D<int>, Vector2D<int>>> _globalMouseMovementListeners = [];
    private readonly Dictionary<string, List<Action<Vector2D<int>, Vector2D<int>>>> _mouseMovementListeners = new();

    private readonly IKeyboard? _keyboard;
    private readonly IMouse? _mouse;
    private string _currentContext = string.Empty;
    private IReadOnlyList<Key> _pressedKeys = Array.Empty<Key>();
    private Vector2D<int> _lastMousePosition = Vector2D<int>.Zero;

    public InputManagerService(RenderContext renderContext)
    {
        _keyboard = renderContext.InputContext.Keyboards[0];
        _mouse = renderContext.InputContext.Mice[0];
    }

    /// <summary>
    /// Gets or sets the current input context.
    /// </summary>
    public string CurrentContext
    {
        get => _currentContext;
        set
        {
            if (_currentContext != value)
            {
                var oldContext = _currentContext;
                _currentContext = value ?? string.Empty;
                ContextChanged?.Invoke(this, new(oldContext, _currentContext));
                _logger.Debug("Input context changed from {OldContext} to {NewContext}", oldContext, _currentContext);
            }
        }
    }

    /// <summary>
    /// Gets or sets the delay in seconds before key repeat starts.
    /// Default is 0.5 seconds (500ms).
    /// </summary>
    public float KeyRepeatDelay { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the interval in seconds between key repeats.
    /// Default is 0.05 seconds (50ms).
    /// </summary>
    public float KeyRepeatInterval { get; set; } = 0.05f;

    /// <summary>
    /// Gets the current keyboard snapshot.
    /// </summary>
    public KeyboardState CurrentKeyboardState { get; private set; }

    /// <summary>
    /// Gets the previous keyboard snapshot.
    /// </summary>
    public KeyboardState PreviousKeyboardState { get; private set; }

    /// <summary>
    /// Gets the current mouse snapshot.
    /// </summary>
    public MouseState CurrentMouseState { get; private set; }

    /// <summary>
    /// Gets the previous mouse snapshot.
    /// </summary>
    public MouseState PreviousMouseState { get; private set; }

    /// <summary>
    /// Gets the current input focus receiver.
    /// </summary>
    public IInputReceiver? CurrentFocus { get; private set; }

    /// <summary>
    /// Gets the depth of the focus stack.
    /// </summary>
    public int FocusStackDepth => _focusStack.Count;

    /// <summary>
    /// Gets a read-only list of currently pressed keys.
    /// </summary>
    public IReadOnlyList<Key> PressedKeys => _pressedKeys;

    /// <summary>
    /// Event raised when the input context changes.
    /// </summary>
    public event EventHandler<InputContextChangedEventArgs>? ContextChanged;

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(string binding, Action action, string? context = null)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);

            return;
        }

        BindKey(keyBinding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(KeyBinding binding, Action action, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        _keyBindings[binding] = (action, context);
        _logger.Debug("Bound key {Binding} to action (context: {Context})", binding, context ?? "any");
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    public void ClearBindings()
    {
        _keyBindings.Clear();
        _logger.Debug("Cleared all key bindings");
    }

    /// <summary>
    /// Clears the current input focus.
    /// </summary>
    public void ClearFocus()
    {
        SetFocus(null);
    }

    /// <summary>
    /// Disposes of the input manager and clears all resources.
    /// </summary>
    public void Dispose()
    {
        _focusStack.Clear();
        _keyBindings.Clear();
        _processedBindings.Clear();
        _keyPressDuration.Clear();
        _keyRepeatTimers.Clear();
        CurrentFocus = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Distributes input to the current focus receiver.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public void DistributeInput(GameTime gameTime)
    {
        if (CurrentFocus != null && CurrentFocus.HasFocus)
        {
            CurrentFocus.HandleKeyboard(CurrentKeyboardState, PreviousKeyboardState, gameTime);
            CurrentFocus.HandleMouse(CurrentMouseState, gameTime);
            CurrentFocus.HandleMouseWheel(CurrentMouseState, PreviousMouseState, gameTime);
        }
    }

    /// <summary>
    /// Gets the duration in seconds that a key has been pressed.
    /// Returns 0 if the key is not currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>Duration in seconds that the key has been pressed.</returns>
    public float GetKeyPressDuration(Key key)
        => _keyPressDuration.TryGetValue(key, out var duration) ? duration : 0f;

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down.</returns>
    public bool IsKeyDown(Key key)
        => CurrentKeyboardState.IsKeyDown(key);

    /// <summary>
    /// Checks if a key is held (pressed and not just pressed this frame).
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down but was already down last frame.</returns>
    public bool IsKeyHeld(Key key)
        => IsKeyDown(key) && !IsKeyPressed(key);

    /// <summary>
    /// Checks if a key has been held for at least the specified duration.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="duration">The minimum duration in seconds.</param>
    /// <returns>True if the key is currently pressed and has been held for at least the duration.</returns>
    public bool IsKeyHeldFor(Key key, float duration)
        => IsKeyDown(key) && GetKeyPressDuration(key) >= duration;

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed.</returns>
    public bool IsKeyPressed(Key key)
        => CurrentKeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyUp(key);

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just released.</returns>
    public bool IsKeyReleased(Key key)
        => CurrentKeyboardState.IsKeyUp(key) && PreviousKeyboardState.IsKeyDown(key);

    /// <summary>
    /// Checks if a key was just pressed or should repeat based on key repeat timing.
    /// Returns true on initial press, then after KeyRepeatDelay, then every KeyRepeatInterval.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed or should repeat.</returns>
    public bool IsKeyRepeated(Key key)
    {
        // Check if the key was just pressed (first frame)
        if (IsKeyPressed(key))
        {
            return true;
        }

        // Check if the key is currently held down
        if (!IsKeyDown(key))
        {
            return false;
        }

        // Check if we have a repeat timer for this key
        if (!_keyRepeatTimers.TryGetValue(key, out var lastRepeatTime))
        {
            return false;
        }

        var pressDuration = GetKeyPressDuration(key);

        // If we haven't reached the initial delay yet, don't repeat
        if (pressDuration < KeyRepeatDelay)
        {
            return false;
        }

        // Calculate time since last repeat
        var timeSinceLastRepeat = pressDuration - lastRepeatTime;

        // Check if it's time for another repeat
        return timeSinceLastRepeat >= KeyRepeatInterval;
    }

    /// <summary>
    /// Checks if a mouse button is currently down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down.</returns>
    public bool IsMouseButtonDown(MouseButton button)
    {
        return button switch
        {
            MouseButton.Left     => CurrentMouseState.LeftButton() == ButtonState.Pressed,
            MouseButton.Right    => CurrentMouseState.RightButton() == ButtonState.Pressed,
            MouseButton.Middle   => CurrentMouseState.MiddleButton() == ButtonState.Pressed,
            MouseButton.XButton1 => CurrentMouseState.XButton1() == ButtonState.Pressed,
            MouseButton.XButton2 => CurrentMouseState.XButton2() == ButtonState.Pressed,
            _                    => false
        };
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

        var wasReleased = button switch
        {
            MouseButton.Left     => PreviousMouseState.LeftButton() == ButtonState.Released,
            MouseButton.Right    => PreviousMouseState.RightButton() == ButtonState.Released,
            MouseButton.Middle   => PreviousMouseState.MiddleButton() == ButtonState.Released,
            MouseButton.XButton1 => PreviousMouseState.XButton1() == ButtonState.Released,
            MouseButton.XButton2 => PreviousMouseState.XButton2() == ButtonState.Released,
            _                    => false
        };

        var isNowPressed = button switch
        {
            MouseButton.Left     => CurrentMouseState.LeftButton() == ButtonState.Pressed,
            MouseButton.Right    => CurrentMouseState.RightButton() == ButtonState.Pressed,
            MouseButton.Middle   => CurrentMouseState.MiddleButton() == ButtonState.Pressed,
            MouseButton.XButton1 => CurrentMouseState.XButton1() == ButtonState.Pressed,
            MouseButton.XButton2 => CurrentMouseState.XButton2() == ButtonState.Pressed,
            _                    => false
        };

        return wasReleased && isNowPressed;
    }

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just released.</returns>
    public bool IsMouseButtonReleased(MouseButton button)
    {
        var wasPressed = button switch
        {
            MouseButton.Left     => PreviousMouseState.LeftButton() == ButtonState.Pressed,
            MouseButton.Right    => PreviousMouseState.RightButton() == ButtonState.Pressed,
            MouseButton.Middle   => PreviousMouseState.MiddleButton() == ButtonState.Pressed,
            MouseButton.XButton1 => PreviousMouseState.XButton1() == ButtonState.Pressed,
            MouseButton.XButton2 => PreviousMouseState.XButton2() == ButtonState.Pressed,
            _                    => false
        };

        var isNowReleased = button switch
        {
            MouseButton.Left     => CurrentMouseState.LeftButton() == ButtonState.Released,
            MouseButton.Right    => CurrentMouseState.RightButton() == ButtonState.Released,
            MouseButton.Middle   => CurrentMouseState.MiddleButton() == ButtonState.Released,
            MouseButton.XButton1 => CurrentMouseState.XButton1() == ButtonState.Released,
            MouseButton.XButton2 => CurrentMouseState.XButton2() == ButtonState.Released,
            _                    => false
        };

        return wasPressed && isNowReleased;
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

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKey(string binding)
    {
        if (!KeyBinding.TryParse(binding, out var keyBinding))
        {
            _logger.Warning("Failed to parse key binding: {Binding}", binding);

            return;
        }

        UnbindKey(keyBinding);
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKey(KeyBinding binding)
    {
        if (_keyBindings.Remove(binding))
        {
            _logger.Debug("Unbound key {Binding}", binding);
        }
    }

    /// <summary>
    /// Updates the input manager by sampling current keyboard and mouse states.
    /// Also processes key bindings and updates key press durations.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
     public void Update(GameTime gameTime)
     {
         PreviousKeyboardState = CurrentKeyboardState;
         PreviousMouseState = CurrentMouseState;

         CurrentKeyboardState = _keyboard.CaptureState();
         CurrentMouseState = _mouse.CaptureState();

         // Update pressed keys list
         _pressedKeys = CurrentKeyboardState.GetPressedKeys().ToArray();

         UpdateKeyPressDuration(gameTime);
         ProcessKeyBindings();
         ProcessMouseMovement();
     }

    /// <summary>
    /// Checks for clicks on focusable elements and automatically sets focus.
    /// </summary>
    /// <param name="receivers">All input receivers to check for mouse clicks.</param>
    public void UpdateFocusFromMouse(IEnumerable<IInputReceiver> receivers)
    {
        var mousePos = new Vector2(CurrentMouseState.Position.X, CurrentMouseState.Position.Y);

        // Check if any focusable element was clicked
        if (IsMouseButtonPressed(MouseButton.Left))
        {
            foreach (var receiver in receivers)
            {
                if (receiver.IsFocusable && receiver.IsMouseInBounds(mousePos))
                {
                    SetFocus(receiver);

                    break;
                }
            }
        }
    }

    /// <summary>
    ///  Gets or sets whether the mouse cursor is visible.
    /// </summary>
    public bool IsMouseVisible { get; set; } = true;

    /// <summary>
    /// Processes all registered key bindings and executes bound actions.
    /// </summary>
    private void ProcessKeyBindings()
    {
        _processedBindings.Clear();

        foreach (var (binding, (action, context)) in _keyBindings)
        {
            // Check if binding is in the correct context
            // If context is null, binding is global (always active)
            // If context is set, only active when CurrentContext matches
            if (context != null && context != CurrentContext)
            {
                continue;
            }

            // Check if the binding was just pressed
            if (binding.IsJustPressed(CurrentKeyboardState, PreviousKeyboardState))
            {
                // Avoid processing the same binding multiple times per frame
                if (_processedBindings.Add(binding))
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error executing key binding action for {Binding}", binding);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates key press duration tracking for all currently pressed keys.
    /// Also updates key repeat timers.
    /// </summary>
    private void UpdateKeyPressDuration(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTimeAsTimeSpan.TotalSeconds;
        var keysToRemove = new List<Key>();

        // Update duration for keys that are currently pressed
        foreach (var key in _keyPressDuration.Keys)
        {
            if (IsKeyDown(key))
            {
                _keyPressDuration[key] += deltaTime;
            }
            else
            {
                keysToRemove.Add(key);
            }
        }

        // Remove keys that were released
        foreach (var key in keysToRemove)
        {
            _keyPressDuration.Remove(key);
            _keyRepeatTimers.Remove(key);
        }

        // Add newly pressed keys
        var pressedKeys = CurrentKeyboardState.GetPressedKeys();

        foreach (var key in pressedKeys)
        {
            if (_keyPressDuration.TryAdd(key, deltaTime))
            {
                // Key was just pressed, initialize repeat timer
                _keyRepeatTimers[key] = 0f;
            }
        }

        // Update repeat timers for keys that should repeat
        foreach (var key in pressedKeys)
         {
             if (_keyRepeatTimers.ContainsKey(key))
             {
                 var pressDuration = _keyPressDuration[key];
                 var lastRepeatTime = _keyRepeatTimers[key];

                 // If we've passed the initial delay and it's time to repeat
                 if (pressDuration >= KeyRepeatDelay)
                 {
                     var timeSinceLastRepeat = pressDuration - lastRepeatTime;

                     if (timeSinceLastRepeat >= KeyRepeatInterval)
                     {
                         // Update the timer to mark that a repeat occurred
                         _keyRepeatTimers[key] = pressDuration;
                     }
                 }
             }
         }
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
            listeners = new List<Action<Vector2D<int>, Vector2D<int>>>();
            _mouseMovementListeners[context] = listeners;
        }

        if (!listeners.Contains(callback))
        {
            listeners.Add(callback);
            _logger.Debug("Bound mouse movement callback to context {Context}", context);
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
    /// Processes mouse movement listeners (both global and context-specific).
    /// Passes both delta and absolute position to callbacks.
    /// </summary>
    private void ProcessMouseMovement()
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
        if (!string.IsNullOrEmpty(_currentContext) && 
            _mouseMovementListeners.TryGetValue(_currentContext, out var listeners))
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
