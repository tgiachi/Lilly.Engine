using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Primitives;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Base class for 2D game objects, providing common functionality for drawing, updating, and transform management.
/// </summary>
public abstract class Base2dGameObject : IGameObject2d, IUpdateble, IInitializable
{
    /// <summary>
    /// Gets or sets the unique identifier for this game object.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this game object.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the Z-index for rendering order.
    /// </summary>
    public uint ZIndex { get; set; }

    private bool _isActive;

    /// <summary>
    /// Gets or sets whether the game object is active and should be updated and drawn.
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;

            foreach (var child in Children)
            {
                child.IsActive = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the parent game object in the hierarchy.
    /// </summary>
    public IGameObject? Parent { get; set; }

    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public IEnumerable<IGameObject> Children { get; } = new GameObjectCollection<IGameObject>();

    public void OnRemoved()
    {
        foreach (var child in Children)
        {
            _gameObjectManager.RemoveGameObject(child);
        }
    }

    /// <summary>
    /// Gets the sprite batcher used for rendering.
    /// </summary>
    protected ILillySpriteBatcher? SpriteBatcher { get; private set; }

    /// <summary>
    /// Gets the 2D transform containing position, rotation, scale, and size.
    /// </summary>
    public Transform2D Transform { get; } = new Transform2D();

    /// <summary>
    /// Optional render context, used to read the current viewport size for anchoring helpers.
    /// </summary>
    protected RenderContext? RenderContext { get; private set; }

    private HorizontalAnchor _horizontalAnchor = HorizontalAnchor.None;
    private VerticalAnchor _verticalAnchor = VerticalAnchor.None;
    private float _horizontalOffset;
    private float _verticalOffset;
    private Vector2 _manualViewportSize = Vector2.Zero;
    private readonly IGameObjectManager _gameObjectManager;

    /// <summary>
    /// Initializes a new instance of the Base2dGameObject class.
    /// </summary>
    /// <param name="name">The name of the game object.</param>
    /// <param name="zIndex">The Z-index for rendering order.</param>
    protected Base2dGameObject(string name, IGameObjectManager gameObjectManager, uint zIndex = 0)
    {
        Name = name;
        _gameObjectManager = gameObjectManager;
        ZIndex = zIndex;
        IsActive = true;
    }

    /// <summary>
    /// Draws the game object and its children if active.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="spriteBatcher">The sprite batcher for rendering.</param>
    public void Draw(GameTime gameTime, ILillySpriteBatcher spriteBatcher)
    {
        if (!IsActive)
        {
            return;
        }

        SpriteBatcher = spriteBatcher;

        ApplyAnchoredLayout();

        OnDraw(gameTime);

        foreach (var child in Children)
        {
            if (child is IGameObject2d child2d)
            {
                child2d.Draw(gameTime, spriteBatcher);
            }
        }
    }

    /// <summary>
    /// Adds a 2D game object as a child.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    protected void AddGameObject2d(params IGameObject2d[] gameObjects)
    {
        ArgumentNullException.ThrowIfNull(gameObjects);

        foreach (var gameObject in gameObjects)
        {
            if (Children is GameObjectCollection<IGameObject> collection)
            {
                collection.Add(gameObject);
                gameObject.Parent = this;
                _gameObjectManager.AddGameObject(gameObject);
            }
        }
    }

    /// <summary>
    /// Removes a 2D game object from the children collection.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    protected void RemoveGameObject2d(IGameObject2d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject> collection)
        {
            if (collection.Remove(gameObject))
            {
                gameObject.Parent = null;
                _gameObjectManager.RemoveGameObject(gameObject);
            }
        }
    }

    /// <summary>
    /// Called when the game object is drawn. Override to implement custom drawing logic.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    protected virtual void OnDraw(GameTime gameTime) { }

    /// <summary>
    /// Updates the game object. Override to implement custom update logic.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public virtual void Update(GameTime gameTime)
    {
        ApplyAnchoredLayout();

        if (!IsActive)
        {
            return;
        }

        foreach (var child in Children)
        {
            if (child is IUpdateble child2d)
            {
                child2d.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Gets the world position by accumulating all parent transforms.
    /// If a parent exists, applies parent's scale and rotation to local position.
    /// </summary>
    public Vector2 GetWorldPosition()
    {
        if (Parent is Base2dGameObject parent2d)
        {
            var parentWorldPos = parent2d.GetWorldPosition();
            var parentRotation = parent2d.GetWorldRotation();
            var parentScale = parent2d.GetWorldScale();

            // Apply parent's scale to local position
            var scaledPos = Transform.Position * parentScale;

            // Rotate the scaled position around parent's origin
            if (parentRotation != 0)
            {
                var cos = MathF.Cos(parentRotation);
                var sin = MathF.Sin(parentRotation);
                var rotatedPos = new Vector2(
                    scaledPos.X * cos - scaledPos.Y * sin,
                    scaledPos.X * sin + scaledPos.Y * cos
                );

                return parentWorldPos + rotatedPos;
            }

            return parentWorldPos + scaledPos;
        }

        return Transform.Position;
    }

    /// <summary>
    /// Gets the world rotation by accumulating all parent rotations.
    /// </summary>
    public float GetWorldRotation()
    {
        if (Parent is Base2dGameObject parent2d)
        {
            return parent2d.GetWorldRotation() + Transform.Rotation;
        }

        return Transform.Rotation;
    }

    /// <summary>
    /// Gets the world scale by multiplying all parent scales.
    /// </summary>
    public Vector2 GetWorldScale()
    {
        if (Parent is Base2dGameObject parent2d)
        {
            return parent2d.GetWorldScale() * Transform.Scale;
        }

        return Transform.Scale;
    }

    /// <summary>
    /// Gets the world size by multiplying the local size by the world scale.
    /// </summary>
    public Vector2 GetWorldSize()
    {
        return Transform.Size * GetWorldScale();
    }

    /// <summary>
    ///  Initializes the game object. Override to implement custom initialization logic.
    /// </summary>
    public virtual void Initialize() { }

    /// <summary>
    /// Sets the render context so the object can react to viewport changes (e.g., ToCenter, ToLeft).
    /// </summary>
    public void UseRenderContext(RenderContext renderContext)
    {
        RenderContext = renderContext;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Allows manual viewport sizing when a render context is not available (tests or offscreen usage).
    /// </summary>
    public void SetViewportSize(Vector2 viewportSize)
    {
        _manualViewportSize = viewportSize;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Anchors the object to the viewport center, with optional offsets.
    /// </summary>
    public void ToCenter(float offsetX = 0f, float offsetY = 0f)
    {
        _horizontalAnchor = HorizontalAnchor.Center;
        _verticalAnchor = VerticalAnchor.Center;
        _horizontalOffset = offsetX;
        _verticalOffset = offsetY;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Anchors the object to the left edge of the viewport.
    /// </summary>
    public void ToLeft(float padding = 0f)
    {
        _horizontalAnchor = HorizontalAnchor.Left;
        _horizontalOffset = padding;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Anchors the object to the right edge of the viewport.
    /// </summary>
    public void ToRight(float padding = 0f)
    {
        _horizontalAnchor = HorizontalAnchor.Right;
        _horizontalOffset = padding;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Anchors the object to the top edge of the viewport.
    /// </summary>
    public void ToTop(float padding = 0f)
    {
        _verticalAnchor = VerticalAnchor.Top;
        _verticalOffset = padding;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Anchors the object to the bottom edge of the viewport.
    /// </summary>
    public void ToBottom(float padding = 0f)
    {
        _verticalAnchor = VerticalAnchor.Bottom;
        _verticalOffset = padding;
        ApplyAnchoredLayout();
    }

    /// <summary>
    /// Removes any active anchoring so manual positioning is respected again.
    /// </summary>
    public void ClearAnchors()
    {
        _horizontalAnchor = HorizontalAnchor.None;
        _verticalAnchor = VerticalAnchor.None;
        _horizontalOffset = 0f;
        _verticalOffset = 0f;
    }

    private void ApplyAnchoredLayout()
    {
        if (_horizontalAnchor == HorizontalAnchor.None && _verticalAnchor == VerticalAnchor.None)
        {
            return;
        }

        var viewportSize = GetViewportSize();

        if (viewportSize == Vector2.Zero)
        {
            return;
        }

        var scaledSize = GetLocalScaledSize();
        var position = Transform.Position;

        switch (_horizontalAnchor)
        {
            case HorizontalAnchor.Left:
                position.X = _horizontalOffset;

                break;
            case HorizontalAnchor.Center:
                position.X = (viewportSize.X - scaledSize.X) / 2f + _horizontalOffset;

                break;
            case HorizontalAnchor.Right:
                position.X = viewportSize.X - scaledSize.X - _horizontalOffset;

                break;
        }

        switch (_verticalAnchor)
        {
            case VerticalAnchor.Top:
                position.Y = _verticalOffset;

                break;
            case VerticalAnchor.Center:
                position.Y = (viewportSize.Y - scaledSize.Y) / 2f + _verticalOffset;

                break;
            case VerticalAnchor.Bottom:
                position.Y = viewportSize.Y - scaledSize.Y - _verticalOffset;

                break;
        }

        Transform.Position = position;
    }

    private Vector2 GetViewportSize()
    {
        if (RenderContext != null)
        {
            var viewport = RenderContext.GraphicsDevice.Viewport;

            return new Vector2(viewport.Width, viewport.Height);
        }

        return _manualViewportSize;
    }

    private Vector2 GetLocalScaledSize()
    {
        var size = Transform.Size;
        var scale = Transform.Scale;

        return new Vector2(size.X * scale.X, size.Y * scale.Y);
    }

    private enum HorizontalAnchor
    {
        None,
        Left,
        Center,
        Right
    }

    private enum VerticalAnchor
    {
        None,
        Top,
        Center,
        Bottom
    }
}
