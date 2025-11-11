using Silk.NET.Input;
using Silk.NET.Input.Extensions;

namespace Lilly.Engine.Rendering.Core.Data.Input;

/// <summary>
/// Represents a key binding with optional modifiers (Ctrl, Shift, Alt).
/// Can be used to define keyboard shortcuts and detect when they are pressed.
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
///     var saveBinding = new KeyBinding(Keys.S, requiresCtrl: true);
///     if (saveBinding.IsJustPressed(currentState, previousState))
///     {
///         SaveGame();
///     }
///     </code>
/// </remarks>
public readonly struct KeyBinding : IEquatable<KeyBinding>
{
    /// <summary>
    /// Gets the primary key for this binding.
    /// </summary>
    public Key Key { get; }

    /// <summary>
    /// Gets whether the Ctrl modifier is required for this binding to activate.
    /// </summary>
    public bool RequiresCtrl { get; }

    /// <summary>
    /// Gets whether the Shift modifier is required for this binding to activate.
    /// </summary>
    public bool RequiresShift { get; }

    /// <summary>
    /// Gets whether the Alt modifier is required for this binding to activate.
    /// </summary>
    public bool RequiresAlt { get; }

    /// <summary>
    /// Initializes a new instance of the KeyBinding struct.
    /// </summary>
    /// <param name="key">The primary key.</param>
    /// <param name="requiresCtrl">Whether the Ctrl modifier is required. Defaults to false.</param>
    /// <param name="requiresShift">Whether the Shift modifier is required. Defaults to false.</param>
    /// <param name="requiresAlt">Whether the Alt modifier is required. Defaults to false.</param>
    /// <remarks>
    /// The binding will only be considered active when:
    /// 1. The primary key is down
    /// 2. All required modifiers are pressed
    /// 3. No other modifiers are pressed (that weren't required)
    /// </remarks>
    public KeyBinding(Key key, bool requiresCtrl = false, bool requiresShift = false, bool requiresAlt = false)
    {
        Key = key;
        RequiresCtrl = requiresCtrl;
        RequiresShift = requiresShift;
        RequiresAlt = requiresAlt;
    }

    /// <summary>
    /// Determines whether the specified KeyBinding is equal to the current KeyBinding.
    /// </summary>
    /// <param name="other">The KeyBinding to compare with the current KeyBinding.</param>
    /// <returns>True if the specified KeyBinding is equal to the current KeyBinding; false otherwise.</returns>
    public bool Equals(KeyBinding other)
        => Key == other.Key &&
           RequiresCtrl == other.RequiresCtrl &&
           RequiresShift == other.RequiresShift &&
           RequiresAlt == other.RequiresAlt;

    /// <summary>
    /// Determines whether the specified object is equal to the current KeyBinding.
    /// </summary>
    /// <param name="obj">The object to compare with the current KeyBinding.</param>
    /// <returns>True if the specified object is equal to the current KeyBinding; false otherwise.</returns>
    public override bool Equals(object? obj)
        => obj is KeyBinding other && Equals(other);

    /// <summary>
    /// Returns the hash code for this key binding.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
        => HashCode.Combine(Key, RequiresCtrl, RequiresShift, RequiresAlt);

    /// <summary>
    /// Checks if this key binding was just pressed (not held from previous frame).
    /// </summary>
    /// <param name="currentState">The current keyboard state snapshot.</param>
    /// <param name="previousState">The previous keyboard state snapshot.</param>
    /// <returns>True if the binding was just pressed this frame using strict matching.</returns>
    /// <remarks>
    /// Uses the strict matching logic from IsPressed. The key must transition from not-pressed to pressed.
    /// </remarks>
    public bool IsJustPressed(KeyboardState currentState, KeyboardState previousState)
        => IsPressed(currentState) && !IsPressed(previousState);

    /// <summary>
    /// Checks if this key binding was just pressed (not held from previous frame), ignoring other modifiers.
    /// </summary>
    /// <param name="currentState">The current keyboard state snapshot.</param>
    /// <param name="previousState">The previous keyboard state snapshot.</param>
    /// <returns>True if the binding was just pressed this frame using loose matching.</returns>
    /// <remarks>
    /// Uses the loose matching logic from IsPressedLoose. The key must transition from not-pressed to pressed.
    /// </remarks>
    public bool IsJustPressedLoose(KeyboardState currentState, KeyboardState previousState)
        => IsPressedLoose(currentState) && !IsPressedLoose(previousState);

    /// <summary>
    /// Checks if this key binding was just released (pressed in previous frame, not in current).
    /// </summary>
    /// <param name="currentState">The current keyboard state snapshot.</param>
    /// <param name="previousState">The previous keyboard state snapshot.</param>
    /// <returns>True if the binding was just released this frame using strict matching.</returns>
    /// <remarks>
    /// The primary key must transition from pressed to not-pressed.
    /// </remarks>
    public bool IsJustReleased(KeyboardState currentState, KeyboardState previousState)
        => !IsPressed(currentState) && IsPressed(previousState);

    /// <summary>
    /// Checks if this key binding was just released (pressed in previous frame, not in current), ignoring other modifiers.
    /// </summary>
    /// <param name="currentState">The current keyboard state snapshot.</param>
    /// <param name="previousState">The previous keyboard state snapshot.</param>
    /// <returns>True if the binding was just released this frame using loose matching.</returns>
    /// <remarks>
    /// Uses the loose matching logic. The primary key must transition from pressed to not-pressed.
    /// </remarks>
    public bool IsJustReleasedLoose(KeyboardState currentState, KeyboardState previousState)
        => !IsPressedLoose(currentState) && IsPressedLoose(previousState);

    /// <summary>
    /// Checks if this key binding matches the current keyboard state exactly.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state snapshot.</param>
    /// <returns>True if the binding is currently pressed and no other modifiers are pressed.</returns>
    /// <remarks>
    /// This method uses strict matching:
    /// - The primary key must be down
    /// - All required modifiers must be pressed
    /// - No unrequired modifiers can be pressed
    /// Example: If binding is "A" (no modifiers), pressing Ctrl+A will return false.
    /// </remarks>
    public bool IsPressed(KeyboardState keyboardState)
    {
        if (!keyboardState.IsKeyPressed(Key))
        {
            return false;
        }

        var ctrlPressed = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
        var shiftPressed = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
        var altPressed = keyboardState.IsKeyPressed(Key.AltLeft) || keyboardState.IsKeyPressed(Key.AltRight);

        // Check that all required modifiers are pressed
        if (RequiresCtrl && !ctrlPressed)
        {
            return false;
        }

        if (RequiresShift && !shiftPressed)
        {
            return false;
        }

        if (RequiresAlt && !altPressed)
        {
            return false;
        }

        // Check that no unrequired modifiers are pressed (strict matching)
        if (!RequiresCtrl && ctrlPressed)
        {
            return false;
        }

        if (!RequiresShift && shiftPressed)
        {
            return false;
        }

        if (!RequiresAlt && altPressed)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if this key binding is currently pressed (held), ignoring other modifiers.
    /// </summary>
    /// <param name="keyboardState">The current keyboard state snapshot.</param>
    /// <returns>True if the binding key and all required modifiers are pressed (other modifiers may also be pressed).</returns>
    /// <remarks>
    /// This method uses loose matching - unrequired modifiers are ignored.
    /// Example: If binding is "A" (no modifiers), pressing Ctrl+A will return true.
    /// </remarks>
    public bool IsPressedLoose(KeyboardState keyboardState)
    {
        if (!keyboardState.IsKeyPressed(Key))
        {
            return false;
        }

        var ctrlPressed = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
        var shiftPressed = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
        var altPressed = keyboardState.IsKeyPressed(Key.AltLeft) || keyboardState.IsKeyPressed(Key.AltRight);

        // Check that all required modifiers are pressed
        if (RequiresCtrl && !ctrlPressed)
        {
            return false;
        }

        if (RequiresShift && !shiftPressed)
        {
            return false;
        }

        if (RequiresAlt && !altPressed)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether two specified KeyBindings are equal.
    /// </summary>
    /// <param name="left">The first KeyBinding to compare.</param>
    /// <param name="right">The second KeyBinding to compare.</param>
    /// <returns>True if left and right are equal; false otherwise.</returns>
    public static bool operator ==(KeyBinding left, KeyBinding right)
        => left.Equals(right);

    /// <summary>
    /// Determines whether two specified KeyBindings are not equal.
    /// </summary>
    /// <param name="left">The first KeyBinding to compare.</param>
    /// <param name="right">The second KeyBinding to compare.</param>
    /// <returns>True if left and right are not equal; false otherwise.</returns>
    public static bool operator !=(KeyBinding left, KeyBinding right)
        => !left.Equals(right);

    /// <summary>
    /// Parses a key binding string (e.g., "Ctrl+A", "Shift+F1", "Ctrl+Shift+S").
    /// </summary>
    /// <param name="binding">The binding string to parse. Format: [Ctrl|Control][+Shift][+Alt]+KeyName</param>
    /// <returns>The parsed key binding.</returns>
    /// <exception cref="ArgumentException">Thrown when binding format is invalid or the key name is not recognized.</exception>
    /// <remarks>
    /// Case insensitive. Valid modifier names: Ctrl, Control, Shift, Alt.
    /// Key names must be valid members of the Keys enum.
    /// </remarks>
    public static KeyBinding Parse(string binding)
    {
        if (string.IsNullOrWhiteSpace(binding))
        {
            throw new ArgumentException("Binding string cannot be null or empty", nameof(binding));
        }

        var parts = binding.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            throw new ArgumentException("Invalid binding format", nameof(binding));
        }

        var requiresCtrl = false;
        var requiresShift = false;
        var requiresAlt = false;
        Key? key = null;

        foreach (var part in parts)
        {
            var upperPart = part.ToUpperInvariant();

            if (upperPart == "CTRL" || upperPart == "CONTROL")
            {
                requiresCtrl = true;
            }
            else if (upperPart == "SHIFT")
            {
                requiresShift = true;
            }
            else if (upperPart == "ALT")
            {
                requiresAlt = true;
            }
            else
            {
                if (Enum.TryParse<Key>(part, true, out var parsedKey))
                {
                    key = parsedKey;
                }
                else
                {
                    throw new ArgumentException($"Invalid key: {part}", nameof(binding));
                }
            }
        }

        if (!key.HasValue)
        {
            throw new ArgumentException("No key specified in binding", nameof(binding));
        }

        return new(key.Value, requiresCtrl, requiresShift, requiresAlt);
    }

    /// <summary>
    /// Returns a string representation of this key binding (e.g., "Ctrl+A", "Shift+F1").
    /// </summary>
    /// <returns>A formatted string representing the key binding.</returns>
    public override string ToString()
    {
        var parts = new List<string>();

        if (RequiresCtrl)
        {
            parts.Add("Ctrl");
        }

        if (RequiresShift)
        {
            parts.Add("Shift");
        }

        if (RequiresAlt)
        {
            parts.Add("Alt");
        }
        parts.Add(Key.ToString());

        return string.Join("+", parts);
    }

    /// <summary>
    /// Attempts to parse a key binding string without throwing exceptions.
    /// </summary>
    /// <param name="binding">The binding string to parse.</param>
    /// <param name="result">The parsed key binding if successful; otherwise, a default key binding with Keys.None.</param>
    /// <returns>True if parsing was successful; false otherwise.</returns>
    /// <remarks>
    /// Use this method when you want to handle invalid input gracefully without exceptions.
    /// </remarks>
    public static bool TryParse(string binding, out KeyBinding result)
    {
        try
        {
            result = Parse(binding);

            return true;
        }
        catch
        {
            result = default;

            return false;
        }
    }
}
