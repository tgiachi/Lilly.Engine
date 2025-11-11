using Lilly.Engine.Rendering.Core.Data.Input;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Extension methods for Silk.NET input types to provide XNA-style API compatibility.
/// </summary>
public static class InputExtensions
{
    /// <summary>
    /// Checks if a key is currently down in the keyboard state.
    /// </summary>
    /// <param name="state">The keyboard state.</param>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is down.</returns>
    public static bool IsKeyDown(this KeyboardState state, Key key)
    {
        return state.IsKeyPressed(key);
    }

    /// <summary>
    /// Checks if a key is currently up in the keyboard state.
    /// </summary>
    /// <param name="state">The keyboard state.</param>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is up.</returns>
    public static bool IsKeyUp(this KeyboardState state, Key key)
    {
        return !state.IsKeyPressed(key);
    }

    /// <summary>
    /// Gets all currently pressed keys from the keyboard state.
    /// </summary>
    /// <param name="state">The keyboard state.</param>
    /// <returns>An enumerable of pressed keys.</returns>
    public static IEnumerable<Key> GetPressedKeys(this KeyboardState state)
    {
        foreach (var key in Enum.GetValues<Key>())
        {
            if (state.IsKeyPressed(key))
            {
                yield return key;
            }
        }
    }

    /// <summary>
    /// Gets the state of the left mouse button.
    /// </summary>
    public static ButtonState LeftButton(this MouseState state)
    {
        return state.IsButtonPressed(Silk.NET.Input.MouseButton.Left)
                   ? ButtonState.Pressed
                   : ButtonState.Released;
    }

    /// <summary>
    /// Gets the state of the right mouse button.
    /// </summary>
    public static ButtonState RightButton(this MouseState state)
    {
        return state.IsButtonPressed(Silk.NET.Input.MouseButton.Right)
                   ? ButtonState.Pressed
                   : ButtonState.Released;
    }

    /// <summary>
    /// Gets the state of the middle mouse button.
    /// </summary>
    public static ButtonState MiddleButton(this MouseState state)
    {
        return state.IsButtonPressed(Silk.NET.Input.MouseButton.Middle)
                   ? ButtonState.Pressed
                   : ButtonState.Released;
    }

    /// <summary>
    /// Gets the state of the first extended mouse button (X1).
    /// </summary>
    public static ButtonState XButton1(this MouseState state)
    {
        return state.IsButtonPressed(Silk.NET.Input.MouseButton.Button4)
                   ? ButtonState.Pressed
                   : ButtonState.Released;
    }

    /// <summary>
    /// Gets the state of the second extended mouse button (X2).
    /// </summary>
    public static ButtonState XButton2(this MouseState state)
    {
        return state.IsButtonPressed(Silk.NET.Input.MouseButton.Button5)
                   ? ButtonState.Pressed
                   : ButtonState.Released;
    }
}
