using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Interfaces.Features;

/// <summary>
/// Defines the contract for objects that can receive keyboard and mouse input events.
/// </summary>
public interface IInputReceiver : IGameObject
{
    /// <summary>
    /// Gets or sets whether this object can receive focus.
    /// </summary>
    bool IsFocusable { get; }

    /// <summary>
    /// Gets or sets whether this object has input focus for keyboard and mouse events.
    /// </summary>
    bool HasFocus { get; set; }

    /// <summary>
    /// Gets the bounds of this UI element for interaction detection.
    /// </summary>
    Rectangle<int> Bounds { get; }

    /// <summary>
    /// Handles keyboard input when the object has focus.
    /// </summary>
    /// <param name="keyboardState">Snapshot of the current keyboard state.</param>
    /// <param name="previousKeyboardState">Snapshot of the previous frame's keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime);

    /// <summary>
    /// Handles mouse input when the object has focus.
    /// </summary>
    /// <param name="mouseState">Snapshot of the current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleMouse(MouseState mouseState, GameTime gameTime);

    /// <summary>
    /// Handles mouse wheel input when the object has focus.
    /// </summary>
    /// <param name="mouseState">Snapshot of the current mouse state.</param>
    /// <param name="previousMouseState">Snapshot of the previous mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime);

    /// <summary>
    /// Checks if the mouse position is within the bounds of this object.
    /// </summary>
    /// <param name="mousePosition">The mouse position to check.</param>
    /// <returns>True if mouse is within bounds, false otherwise.</returns>
    bool IsMouseInBounds(Vector2 mousePosition);
}
