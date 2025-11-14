using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Events;
using Lilly.Engine.Rendering.Core.Data.Input;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.Rendering.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for input management services.
/// Handles focus management, input distribution, and key bindings.
/// </summary>
public interface IInputManagerService : IDisposable
{
    /// <summary>
    /// Gets or sets the current input context.
    /// </summary>
    string CurrentContext { get; set; }

    /// <summary>
    /// Gets or sets the delay in seconds before key repeat starts.
    /// Default is 0.5 seconds (500ms).
    /// </summary>
    float KeyRepeatDelay { get; set; }

    /// <summary>
    /// Gets or sets the interval in seconds between key repeats.
    /// Default is 0.05 seconds (50ms).
    /// </summary>
    float KeyRepeatInterval { get; set; }

    /// <summary>
    /// Gets the current keyboard snapshot.
    /// </summary>
    KeyboardState? CurrentKeyboardState { get; }

    /// <summary>
    /// Gets the previous keyboard snapshot.
    /// </summary>
    KeyboardState? PreviousKeyboardState { get; }

    /// <summary>
    /// Gets the current mouse snapshot.
    /// </summary>
    MouseState? CurrentMouseState { get; }

    /// <summary>
    /// Gets the previous mouse snapshot.
    /// </summary>
    MouseState? PreviousMouseState { get; }

    /// <summary>
    /// Gets the current input focus receiver.
    /// </summary>
    IInputReceiver? CurrentFocus { get; }

    /// <summary>
    /// Gets the depth of the focus stack.
    /// </summary>
    int FocusStackDepth { get; }

    /// <summary>
    /// Gets a read-only list of currently pressed keys.
    /// </summary>
    IReadOnlyList<Key> PressedKeys { get; }

    /// <summary>
    /// Event raised when the input context changes.
    /// </summary>
    event EventHandler<InputContextChangedEventArgs>? ContextChanged;

    /// <summary>
    /// Gets or sets whether the mouse cursor is visible.
    /// </summary>
    bool IsMouseVisible { get; set; }

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding string (e.g., "Ctrl+A", "Shift+F1").</param>
    /// <param name="action">The action to execute when the binding is pressed.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKey(string binding, Action action, string? context = null);

    /// <summary>
    /// Binds a key combination to an action.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute when the binding is pressed.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKey(KeyBinding binding, Action action, string? context = null);

    /// <summary>
    /// Binds a key combination to an action that executes every frame while held.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKeyHeld(string binding, Action action, string? context = null);

    /// <summary>
    /// Binds a key combination to an action that executes every frame while held.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKeyHeld(KeyBinding binding, Action action, string? context = null);

    /// <summary>
    /// Binds a key combination to an action that executes with key repeat (initial press + delay + interval).
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKeyRepeat(string binding, Action action, string? context = null);

    /// <summary>
    /// Binds a key combination to an action that executes with key repeat (initial press + delay + interval).
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    void BindKeyRepeat(KeyBinding binding, Action action, string? context = null);

    /// <summary>
    /// Binds a callback to mouse click events globally (always active, regardless of context).
    /// Callback receives the mouse button and mouse position when clicked.
    /// </summary>
    /// <param name="callback">The callback to execute when a mouse button is clicked.</param>
    void BindMouseClick(Action<MouseButton, Vector2D<int>> callback);

    /// <summary>
    /// Binds a callback to mouse click events, only active in a specific context.
    /// Callback receives the mouse button and mouse position when clicked.
    /// </summary>
    /// <param name="callback">The callback to execute when a mouse button is clicked.</param>
    /// <param name="context">The context in which this callback is active.</param>
    void BindMouseClick(Action<MouseButton, Vector2D<int>> callback, string context);

    /// <summary>
    /// Binds a callback to mouse movement globally (always active, regardless of context).
    /// </summary>
    /// <param name="callback">The callback to execute every frame mouse moves.</param>
    void BindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback);

    /// <summary>
    /// Binds a callback to mouse movement, only active in a specific context.
    /// </summary>
    /// <param name="callback">The callback to execute every frame mouse moves.</param>
    /// <param name="context">The context in which this callback is active.</param>
    void BindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback, string context);

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    void ClearBindings();

    /// <summary>
    /// Clears the current input focus.
    /// </summary>
    void ClearFocus();

    /// <summary>
    /// Distributes input to the current focus receiver.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    void DistributeInput(GameTime gameTime);

    /// <summary>
    /// Gets the duration in seconds that a key has been pressed.
    /// Returns 0 if the key is not currently pressed.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>Duration in seconds that the key has been pressed.</returns>
    float GetKeyPressDuration(Key key);

    /// <summary>
    /// Gets the mouse delta (movement since last frame).
    /// </summary>
    /// <returns>The mouse delta as a Vector2D with X and Y components.</returns>
    Vector2D<int> GetMouseDelta();

    /// <summary>
    /// Checks if a key is currently down.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down.</returns>
    bool IsKeyDown(Key key);

    /// <summary>
    /// Checks if a key is held (pressed and not just pressed this frame).
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down but was already down last frame.</returns>
    bool IsKeyHeld(Key key);

    /// <summary>
    /// Checks if a key has been held for at least the specified duration.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="duration">The minimum duration in seconds.</param>
    /// <returns>True if the key is currently pressed and has been held for at least the duration.</returns>
    bool IsKeyHeldFor(Key key, float duration);

    /// <summary>
    /// Checks if a key was just pressed this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed.</returns>
    bool IsKeyPressed(Key key);

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just released.</returns>
    bool IsKeyReleased(Key key);

    /// <summary>
    /// Checks if a key was just pressed or should repeat based on key repeat timing.
    /// Returns true on initial press, then after KeyRepeatDelay, then every KeyRepeatInterval.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just pressed or should repeat.</returns>
    bool IsKeyRepeated(Key key);

    /// <summary>
    /// Checks if a mouse button is currently down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down.</returns>
    bool IsMouseButtonDown(MouseButton button);

    /// <summary>
    /// Checks if a mouse button was just pressed this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just pressed.</returns>
    bool IsMouseButtonPressed(MouseButton button);

    /// <summary>
    /// Checks if a mouse button was just released this frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was just released.</returns>
    bool IsMouseButtonReleased(MouseButton button);

    /// <summary>
    /// Pops the top focus receiver from the stack.
    /// Restores focus to the previous receiver.
    /// </summary>
    void PopFocusStack();

    /// <summary>
    /// Pushes a new focus receiver onto the focus stack.
    /// Used for modal dialogs, menus, etc.
    /// </summary>
    /// <param name="receiver">The receiver to push.</param>
    void PushFocusStack(IInputReceiver receiver);

    /// <summary>
    /// Sets the input focus to a specific receiver.
    /// Clears focus from the previous receiver.
    /// </summary>
    /// <param name="receiver">The receiver to give focus to.</param>
    void SetFocus(IInputReceiver? receiver);

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    void UnbindKey(string binding);

    /// <summary>
    /// Unbinds a key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    void UnbindKey(KeyBinding binding);

    /// <summary>
    /// Unbinds a held key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    void UnbindKeyHeld(string binding);

    /// <summary>
    /// Unbinds a held key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    void UnbindKeyHeld(KeyBinding binding);

    /// <summary>
    /// Unbinds a repeat key combination.
    /// </summary>
    /// <param name="binding">The key binding string.</param>
    void UnbindKeyRepeat(string binding);

    /// <summary>
    /// Unbinds a repeat key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    void UnbindKeyRepeat(KeyBinding binding);

    /// <summary>
    /// Unbinds a global mouse click callback.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    void UnbindMouseClick(Action<MouseButton, Vector2D<int>> callback);

    /// <summary>
    /// Unbinds a mouse click callback for a specific context.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    /// <param name="context">The context from which to remove the binding.</param>
    void UnbindMouseClick(Action<MouseButton, Vector2D<int>> callback, string context);

    /// <summary>
    /// Unbinds a global mouse movement callback.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    void UnbindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback);

    /// <summary>
    /// Unbinds a mouse movement callback for a specific context.
    /// </summary>
    /// <param name="callback">The callback to remove.</param>
    /// <param name="context">The context from which to remove the binding.</param>
    void UnbindMouseMovement(Action<Vector2D<int>, Vector2D<int>> callback, string context);

    /// <summary>
    /// Updates the input manager.
    /// Samples input states and processes key bindings.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    void Update(GameTime gameTime);

    /// <summary>
    /// Checks for clicks on focusable elements and automatically sets focus.
    /// </summary>
    /// <param name="receivers">All input receivers to check for mouse clicks.</param>
    void UpdateFocusFromMouse(IEnumerable<IInputReceiver> receivers);
}
