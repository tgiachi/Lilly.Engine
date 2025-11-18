using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Events;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Data.Input;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.Services.Input;

/// <summary>
/// Implements the input management service using Silk.NET Input.
/// Handles keyboard and mouse input, key bindings, and input focus management.
/// </summary>
public class InputManagerService : IInputManagerService
{
    private readonly ILogger _logger = Log.ForContext<InputManagerService>();
    private readonly KeyboardInputHandler _keyboardHandler;
    private readonly MouseInputHandler _mouseHandler;
    private readonly FocusManager _focusManager;
    private readonly BindingManager _bindingManager;

    private string _currentContext = string.Empty;

    public InputManagerService(RenderContext renderContext)
    {
        _keyboardHandler = new(renderContext.InputContext.Keyboards[0]);
        _mouseHandler = new(renderContext.InputContext.Mice[0]);
        _focusManager = new();
        _bindingManager = new(_keyboardHandler);
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
    public float KeyRepeatDelay
    {
        get => _keyboardHandler.KeyRepeatDelay;
        set => _keyboardHandler.KeyRepeatDelay = value;
    }

    /// <summary>
    /// Gets or sets the interval in seconds between key repeats.
    /// Default is 0.05 seconds (50ms).
    /// </summary>
    public float KeyRepeatInterval
    {
        get => _keyboardHandler.KeyRepeatInterval;
        set => _keyboardHandler.KeyRepeatInterval = value;
    }

    /// <summary>
    /// Gets the current keyboard snapshot.
    /// </summary>
    public KeyboardState CurrentKeyboardState => _keyboardHandler.CurrentKeyboardState;

    /// <summary>
    /// Gets the previous keyboard snapshot.
    /// </summary>
    public KeyboardState PreviousKeyboardState => _keyboardHandler.PreviousKeyboardState;

    /// <summary>
    /// Gets the current mouse snapshot.
    /// </summary>
    public MouseState CurrentMouseState => _mouseHandler.CurrentMouseState;

    /// <summary>
    /// Gets the previous mouse snapshot.
    /// </summary>
    public MouseState PreviousMouseState => _mouseHandler.PreviousMouseState;

    /// <summary>
    /// Gets the current input focus receiver.
    /// </summary>
    public IInputReceiver? CurrentFocus => _focusManager.CurrentFocus;

    /// <summary>
    /// Gets the depth of the focus stack.
    /// </summary>
    public int FocusStackDepth => _focusManager.FocusStackDepth;

    /// <summary>
    /// Gets a read-only list of currently pressed keys.
    /// </summary>
    public IReadOnlyList<Key> PressedKeys => _keyboardHandler.PressedKeys;

    /// <summary>
    /// Event raised when the input context changes.
    /// </summary>
    public event EventHandler<InputContextChangedEventArgs>? ContextChanged;

    /// <summary>
    /// Gets or sets whether the mouse cursor is visible.
    /// </summary>
    public bool IsMouseVisible { get; set; } = true;

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(string binding, Action action, string? context = null)
    {
        _bindingManager.BindKey(binding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKey(KeyBinding binding, Action action, string? context = null)
    {
        _keyboardHandler.BindKey(binding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action that executes every frame while held.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyHeld(string binding, Action action, string? context = null)
    {
        _bindingManager.BindKeyHeld(binding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action that executes every frame while held.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyHeld(KeyBinding binding, Action action, string? context = null)
    {
        _keyboardHandler.BindKeyHeld(binding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action that executes with key repeat (initial press + delay + interval).
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyRepeat(string binding, Action action, string? context = null)
    {
        _bindingManager.BindKeyRepeat(binding, action, context);
    }

    /// <summary>
    /// Binds a key combination to an action that executes with key repeat (initial press + delay + interval).
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyRepeat(KeyBinding binding, Action action, string? context = null)
    {
        _keyboardHandler.BindKeyRepeat(binding, action, context);
    }

    /// <summary>
    /// Binds a callback to mouse click events globally (always active, regardless of context).
    /// </summary>
    /// <param name="callback">The callback to execute when a mouse button is clicked.</param>
    public void BindMouseClick(Action<MouseButton, Vector2D<int>> callback)
    {
        _mouseHandler.BindMouseClick(callback);
    }

    /// <summary>
    /// Binds a callback to mouse click events, only active in a specific context.
    /// </summary>
    /// <param name="callback">The callback to execute when a mouse button is clicked.</param>
    /// <param name="context">The context in which this callback is active.</param>
    public void BindMouseClick(Action<MouseButton, Vector2D<int>> callback, string context)
    {
        _mouseHandler.BindMouseClick(callback, context);
    }

    /// <summary>
    /// Binds a callback to mouse movement globally (always active, regardless of context).
    /// </summary>
    /// <param name="callback">The callback to execute every frame mouse moves.</param>
    public void BindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback)
    {
        _mouseHandler.BindMouseMovement(callback);
    }

    /// <summary>
    /// Binds a callback to mouse movement, only active in a specific context.
    /// </summary>
    /// <param name="callback">The callback to execute every frame mouse moves.</param>
    /// <param name="context">The context in which this callback is active.</param>
    public void BindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback, string context)
    {
        _mouseHandler.BindMouseMovement(callback, context);
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    public void ClearBindings()
    {
        _bindingManager.ClearBindings();
    }

    /// <summary>
    /// Clears the current input focus.
    /// </summary>
    public void ClearFocus()
    {
        _focusManager.ClearFocus();
    }

    /// <summary>
    /// Disposes of the input manager and clears all resources.
    /// </summary>
    public void Dispose()
    {
        _keyboardHandler.Dispose();
        _mouseHandler.Dispose();
        _focusManager.Dispose();
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
        => _keyboardHandler.GetKeyPressDuration(key);

    /// <summary>
    /// Gets the mouse delta (movement since last frame).
    /// </summary>
    /// <returns>The mouse delta as a Vector2D with X and Y components.</returns>
    public Vector2D<int> GetMouseDelta()
        => _mouseHandler.GetMouseDelta();

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down.</returns>
    public bool IsKeyDown(Key key)
        => _keyboardHandler.IsKeyDown(key);

    /// <summary>
    /// Checks if a key is held (pressed and not just pressed this frame).
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down but was already down last frame.</returns>
    public bool IsKeyHeld(Key key)
        => _keyboardHandler.IsKeyHeld(key);

    /// <summary>
    /// Checks if a key has been held for at least the specified duration.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="duration">The minimum duration in seconds.</param>
    /// <returns>True if the key is currently pressed and has been held for at least the duration.</returns>
    public bool IsKeyHeldFor(Key key, float duration)
        => _keyboardHandler.IsKeyHeldFor(key, duration);

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed.</returns>
    public bool IsKeyPressed(Key key)
        => _keyboardHandler.IsKeyPressed(key);

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just released.</returns>
    public bool IsKeyReleased(Key key)
        => _keyboardHandler.IsKeyReleased(key);

    /// <summary>
    /// Checks if a key was just pressed or should repeat based on key repeat timing.
    /// Returns true on initial press, then after KeyRepeatDelay, then every KeyRepeatInterval.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed or should repeat.</returns>
    public bool IsKeyRepeated(Key key)
        => _keyboardHandler.IsKeyRepeated(key);

    /// <summary>
    /// Checks if a mouse button is currently down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down.</returns>
    public bool IsMouseButtonDown(MouseButton button)
        => _mouseHandler.IsMouseButtonDown(button);

    /// <summary>
    /// Checks if a mouse button was just pressed this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just pressed.</returns>
    public bool IsMouseButtonPressed(MouseButton button)
        => _mouseHandler.IsMouseButtonPressed(button);

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just released.</returns>
    public bool IsMouseButtonReleased(MouseButton button)
        => _mouseHandler.IsMouseButtonReleased(button);

    /// <summary>
    /// Pops the top focus receiver from the stack.
    /// </summary>
    public void PopFocusStack()
    {
        _focusManager.PopFocusStack();
    }

    /// <summary>
    /// Pushes a new focus receiver onto the focus stack.
    /// </summary>
    /// <param name="receiver">The receiver to push.</param>
    public void PushFocusStack(IInputReceiver receiver)
    {
        _focusManager.PushFocusStack(receiver);
    }

    /// <summary>
    /// Sets the input focus to a specific receiver.
    /// </summary>
    /// <param name="receiver">The receiver to give focus to.</param>
    public void SetFocus(IInputReceiver? receiver)
    {
        _focusManager.SetFocus(receiver);
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKey(string binding)
    {
        _bindingManager.UnbindKey(binding);
    }

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKey(KeyBinding binding)
    {
        _keyboardHandler.UnbindKey(binding);
    }

    /// <summary>
    /// Unbinds a held key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKeyHeld(string binding)
    {
        _bindingManager.UnbindKeyHeld(binding);
    }

    /// <summary>
    /// Unbinds a held key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKeyHeld(KeyBinding binding)
    {
        _keyboardHandler.UnbindKeyHeld(binding);
    }

    /// <summary>
    /// Unbinds a repeat key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    public void UnbindKeyRepeat(string binding)
    {
        _bindingManager.UnbindKeyRepeat(binding);
    }

    /// <summary>
    /// Unbinds a repeat key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKeyRepeat(KeyBinding binding)
    {
        _keyboardHandler.UnbindKeyRepeat(binding);
    }

    /// <summary>
    /// Unbinds a global mouse click callback.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    public void UnbindMouseClick(Action<MouseButton, Vector2D<int>> callback)
    {
        _mouseHandler.UnbindMouseClick(callback);
    }

    /// <summary>
    /// Unbinds a mouse click callback for a specific context.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    /// <param name="context">The context from which to remove the binding.</param>
    public void UnbindMouseClick(Action<MouseButton, Vector2D<int>> callback, string context)
    {
        _mouseHandler.UnbindMouseClick(callback, context);
    }

    /// <summary>
    /// Unbinds a global mouse movement callback.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    public void UnbindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback)
    {
        _mouseHandler.UnbindMouseMovement(callback);
    }

    /// <summary>
    /// Unbinds a mouse movement callback for a specific context.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    /// <param name="context">The context from which to remove the binding.</param>
    public void UnbindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback, string context)
    {
        _mouseHandler.UnbindMouseMovement(callback, context);
    }

    /// <summary>
    /// Updates the input manager by sampling current keyboard and mouse states.
    /// Also processes key bindings and updates key press durations.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    public void Update(GameTime gameTime)
    {
        _keyboardHandler.Update(gameTime, _currentContext);
        _mouseHandler.Update(_currentContext);
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
}
