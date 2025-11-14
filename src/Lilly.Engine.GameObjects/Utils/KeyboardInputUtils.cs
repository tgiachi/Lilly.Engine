using Silk.NET.Input;

namespace Lilly.Engine.GameObjects.Utils;

/// <summary>
/// Utility class for keyboard input conversion and handling.
/// </summary>
public static class KeyboardInputUtils
{
    /// <summary>
    /// Converts a keyboard key to its character representation.
    /// Supports letters, numbers, numpad, and special characters.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <param name="shift">Whether shift is pressed.</param>
    /// <returns>The character representation, or null if the key has no character mapping.</returns>
    public static char? KeyToChar(Key key, bool shift)
    {
        // Letters (A-Z)
        if (key >= Key.A && key <= Key.Z)
        {
            var offset = key - Key.A;

            return shift ? (char)('A' + offset) : (char)('a' + offset);
        }

        // Numbers (0-9)
        if (key >= Key.Number0 && key <= Key.Number9)
        {
            if (shift)
            {
                return key switch
                {
                    Key.Number1 => '!',
                    Key.Number2 => '@',
                    Key.Number3 => '#',
                    Key.Number4 => '$',
                    Key.Number5 => '%',
                    Key.Number6 => '^',
                    Key.Number7 => '&',
                    Key.Number8 => '*',
                    Key.Number9 => '(',
                    Key.Number0 => ')',
                    _           => null
                };
            }

            var offset = key - Key.Number0;

            return (char)('0' + offset);
        }

        // Numpad (0-9)
        if (key >= Key.Keypad0 && key <= Key.Keypad9)
        {
            var offset = key - Key.Keypad0;

            return (char)('0' + offset);
        }

        // Space
        if (key == Key.Space)
        {
            return ' ';
        }

        // Special characters
        return key switch
        {
            Key.Minus        => shift ? '_' : '-',
            Key.Equal        => shift ? '+' : '=',
            Key.LeftBracket  => shift ? '{' : '[',
            Key.RightBracket => shift ? '}' : ']',
            Key.Semicolon    => shift ? ':' : ';',
            Key.Apostrophe   => shift ? '"' : '\'',
            Key.Comma        => shift ? '<' : ',',
            Key.Period       => shift ? '>' : '.',
            Key.Slash        => shift ? '?' : '/',
            Key.BackSlash    => shift ? '|' : '\\',
            Key.GraveAccent  => shift ? '~' : '`',
            _                => null
        };
    }

    /// <summary>
    /// Converts a keyboard key to its string representation.
    /// Wrapper around KeyToChar for convenience.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <param name="shift">Whether shift is pressed.</param>
    /// <returns>The string representation, or empty string if the key has no character mapping.</returns>
    public static string KeyToString(Key key, bool shift)
        => KeyToChar(key, shift)?.ToString() ?? string.Empty;
}
