using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Primitives;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;

namespace Lilly.Engine.GameObjects.UI.Base;

/// <summary>
/// Base class for all UI game objects.
/// Provides input handling, focus management, and basic UI properties.
/// </summary>
public abstract class BaseUIGameObject : IInputReceiver, IGameObject2D
{
    private readonly IInputManagerService _inputManager;
    private bool _hasFocus;

    /// <summary>
    /// Initializes a new instance of the BaseUIGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    protected BaseUIGameObject(IInputManagerService inputManager)
    {
        _inputManager = inputManager;
    }

    /// <summary>
    /// Gets or sets the unique identifier for this game object.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this game object.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the rendering order.
    /// </summary>
    public ushort Order { get; protected set; }

    /// <summary>
    /// Gets or sets the render layer for this game object.
    /// </summary>
    public RenderLayer Layer { get; set; } = RenderLayer.UI;

    /// <summary>
    /// Gets or sets the parent game object.
    /// </summary>
    public IGameObject? Parent { get; set; }

    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public GameObjectCollection<IGameObject> Children { get; } = new();

    /// <summary>
    /// Gets the 2D transformation for this UI element.
    /// </summary>
    public Transform2D Transform { get; } = new();

    /// <summary>
    /// Gets or sets whether this UI element is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the layer for depth sorting.
    /// </summary>
    int IGameObject2D.Layer { get; set; }

    /// <summary>
    /// Gets or sets whether this UI element can receive focus.
    /// Default is true for UI elements.
    /// </summary>
    public virtual bool IsFocusable { get; protected set; } = true;

    /// <summary>
    /// Gets or sets whether this UI element has input focus.
    /// </summary>
    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus != value)
            {
                _hasFocus = value;
                OnFocusChanged(value);
            }
        }
    }

    /// <summary>
    /// Gets the bounds of this UI element for interaction detection.
    /// </summary>
    public abstract Rectangle<int> Bounds { get; }

    /// <summary>
    /// Handles keyboard input when the UI element has focus.
    /// </summary>
    /// <param name="keyboardState">Current keyboard state.</param>
    /// <param name="previousKeyboardState">Previous keyboard state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        // Base implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Handles mouse input when the UI element has focus.
    /// </summary>
    /// <param name="mouseState">Current mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        // Base implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Handles mouse wheel input when the UI element has focus.
    /// </summary>
    /// <param name="mouseState">Current mouse state.</param>
    /// <param name="previousMouseState">Previous mouse state.</param>
    /// <param name="gameTime">Game timing information.</param>
    public virtual void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Base implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Checks if the mouse position is within the bounds of this UI element.
    /// </summary>
    /// <param name="mousePosition">The mouse position to check.</param>
    /// <returns>True if mouse is within bounds, false otherwise.</returns>
    public virtual bool IsMouseInBounds(Vector2 mousePosition)
    {
        return Bounds.Contains(new Vector2D<int>((int)mousePosition.X, (int)mousePosition.Y));
    }

    /// <summary>
    /// Requests focus for this UI element.
    /// </summary>
    public void RequestFocus()
    {
        if (IsFocusable)
        {
            _inputManager.SetFocus(this);
        }
    }

    /// <summary>
    /// Releases focus from this UI element.
    /// </summary>
    public void ReleaseFocus()
    {
        if (HasFocus)
        {
            _inputManager.ClearFocus();
        }
    }

    /// <summary>
    /// Called when the focus state changes.
    /// Override this to respond to focus changes.
    /// </summary>
    /// <param name="hasFocus">True if the element gained focus, false if it lost focus.</param>
    protected virtual void OnFocusChanged(bool hasFocus)
    {
        // Base implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Renders the UI element.
    /// Must be implemented by derived classes.
    /// </summary>
    /// <param name="gameTime">Game timing information.</param>
    /// <returns>Render commands to execute.</returns>
    public abstract IEnumerable<RenderCommand> Render(GameTime gameTime);

    /// <summary>
    /// Gets the input manager service.
    /// </summary>
    protected IInputManagerService InputManager => _inputManager;




    /// <summary>
    /// Disposes of this UI element and releases all resources.
    /// </summary>
    public virtual void Dispose()
    {
        if (HasFocus)
        {
            ReleaseFocus();
        }

        GC.SuppressFinalize(this);
    }
}
