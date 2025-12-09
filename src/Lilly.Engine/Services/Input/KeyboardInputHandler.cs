using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Data.Input;
using Serilog;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;

namespace Lilly.Engine.Services.Input;

/// <summary>
/// Handles keyboard input states, key bindings, and key press logic.
/// </summary>
internal sealed class KeyboardInputHandler
{
    private readonly ILogger _logger = Log.ForContext<KeyboardInputHandler>();
    private readonly Dictionary<KeyBinding, (Action Action, string? Context)> _keyBindings = new();
    private readonly Dictionary<KeyBinding, (Action Action, string? Context)> _keyHeldBindings = new();
    private readonly Dictionary<KeyBinding, (Action Action, string? Context)> _keyRepeatBindings = new();
    private readonly HashSet<KeyBinding> _processedBindings = [];
    private readonly Dictionary<Key, float> _keyPressDuration = new();
    private readonly Dictionary<Key, float> _keyRepeatTimers = new();

    private readonly IKeyboard _keyboard;

    public KeyboardInputHandler(IKeyboard keyboard)
        => _keyboard = keyboard;

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
    /// Gets a read-only list of currently pressed keys.
    /// </summary>
    public IReadOnlyList<Key> PressedKeys { get; private set; } = Array.Empty<Key>();

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
    /// Binds a key combination to an action that executes every frame while held.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyHeld(KeyBinding binding, Action action, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        _keyHeldBindings[binding] = (action, context);
        _logger.Debug("Bound key held {Binding} to action (context: {Context})", binding, context ?? "any");
    }

    /// <summary>
    /// Binds a key combination to an action that executes with key repeat (initial press + delay + interval).
    /// </summary>
    /// <param name="binding">The key binding.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context to limit when this binding is active.</param>
    public void BindKeyRepeat(KeyBinding binding, Action action, string? context = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        _keyRepeatBindings[binding] = (action, context);
        _logger.Debug("Bound key repeat {Binding} to action (context: {Context})", binding, context ?? "any");
    }

    /// <summary>
    /// Clears all key bindings.
    /// </summary>
    public void ClearBindings()
    {
        _keyBindings.Clear();
        _keyHeldBindings.Clear();
        _keyRepeatBindings.Clear();
        _logger.Debug("Cleared all key bindings");
    }

    /// <summary>
    /// Disposes of keyboard-related resources.
    /// </summary>
    public void Dispose()
    {
        _keyBindings.Clear();
        _keyHeldBindings.Clear();
        _keyRepeatBindings.Clear();
        _processedBindings.Clear();
        _keyPressDuration.Clear();
        _keyRepeatTimers.Clear();
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
        => CurrentKeyboardState.GetPressedKeys().ToArray().Contains(key);

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
        => IsKeyDown(key) && !PreviousKeyboardState.GetPressedKeys().ToArray().Contains(key);

    /// <summary>
    /// Checks if a key was just released this frame.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key was just released.</returns>
    public bool IsKeyReleased(Key key)
        => !IsKeyDown(key) && PreviousKeyboardState.GetPressedKeys().ToArray().Contains(key);

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
    /// Unbinds a held key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKeyHeld(KeyBinding binding)
    {
        if (_keyHeldBindings.Remove(binding))
        {
            _logger.Debug("Unbound held key {Binding}", binding);
        }
    }

    /// <summary>
    /// Unbinds a repeat key combination.
    /// </summary>
    /// <param name="binding">The key binding.</param>
    public void UnbindKeyRepeat(KeyBinding binding)
    {
        if (_keyRepeatBindings.Remove(binding))
        {
            _logger.Debug("Unbound repeat key {Binding}", binding);
        }
    }

    /// <summary>
    /// Updates the keyboard state and processes bindings.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    /// <param name="currentContext">The current input context.</param>
    public void Update(GameTime gameTime, string currentContext)
    {
        PreviousKeyboardState = CurrentKeyboardState;
        CurrentKeyboardState = _keyboard.CaptureState();

        // Update pressed keys list
        PressedKeys = CurrentKeyboardState.GetPressedKeys().ToArray();

        UpdateKeyPressDuration(gameTime);
        ProcessKeyBindings(currentContext);
        ProcessKeyHeldBindings(currentContext);
        ProcessKeyRepeatBindings(currentContext);
    }

    /// <summary>
    /// Processes all registered key bindings and executes bound actions.
    /// </summary>
    /// <param name="currentContext">The current input context.</param>
    private void ProcessKeyBindings(string currentContext)
    {
        _processedBindings.Clear();

        foreach (var (binding, (action, context)) in _keyBindings)
        {
            // Check if binding is in the correct context
            // If context is null, binding is global (always active)
            // If context is set, only active when CurrentContext matches
            if (context != null && context != currentContext)
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
    /// Processes all registered held key bindings and executes bound actions every frame while held.
    /// </summary>
    /// <param name="currentContext">The current input context.</param>
    private void ProcessKeyHeldBindings(string currentContext)
    {
        foreach (var (binding, (action, context)) in _keyHeldBindings)
        {
            // Check if binding is in the correct context
            if (context != null && context != currentContext)
            {
                continue;
            }

            // Check if the binding is currently pressed (held down)
            if (binding.IsPressed(CurrentKeyboardState))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing held key binding action for {Binding}", binding);
                }
            }
        }
    }

    /// <summary>
    /// Processes all registered repeat key bindings and executes bound actions with key repeat timing.
    /// </summary>
    /// <param name="currentContext">The current input context.</param>
    private void ProcessKeyRepeatBindings(string currentContext)
    {
        foreach (var (binding, (action, context)) in _keyRepeatBindings)
        {
            // Check if binding is in the correct context
            if (context != null && context != currentContext)
            {
                continue;
            }

            // Check if the binding should repeat (initial press + delay + interval)
            // Check if the main key should repeat and all modifiers are pressed
            var shouldRepeat = IsKeyRepeated(binding.Key) &&
                               binding.IsPressed(CurrentKeyboardState);

            if (shouldRepeat)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error executing repeat key binding action for {Binding}", binding);
                }
            }
        }
    }

    /// <summary>
    /// Updates key press duration tracking for all currently pressed keys.
    /// Also updates key repeat timers.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
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
            if (_keyRepeatTimers.TryGetValue(key, out var lastRepeatTime))
            {
                var pressDuration = _keyPressDuration[key];

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
}
