using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Base class for 2D game objects, providing common functionality for drawing, updating, and transform management.
/// </summary>
public abstract class Base2dGameObject : IGameObject2d, IUpdateble
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
    /// <summary>
    /// Gets or sets whether the game object is active and should be updated and drawn.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the parent game object in the hierarchy.
    /// </summary>
    public IGameObject? Parent { get; set; }
    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public IEnumerable<IGameObject> Children { get; } = new GameObjectCollection<IGameObject2d>();

    /// <summary>
    /// Gets the sprite batcher used for rendering.
    /// </summary>
    protected ILillySpriteBatcher? SpriteBatcher { get; private set; }

    /// <summary>
    /// Gets the 2D transform containing position, rotation, scale, and size.
    /// </summary>
    public Transform2D Transform { get; } = new Transform2D();

    /// <summary>
    /// Initializes a new instance of the Base2dGameObject class.
    /// </summary>
    /// <param name="name">The name of the game object.</param>
    /// <param name="zIndex">The Z-index for rendering order.</param>
    protected Base2dGameObject(string name, uint zIndex = 0)
    {
        Name = name;
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
    protected void AddGameObject2d(IGameObject2d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject2d> collection)
        {
            collection.Add(gameObject);
            gameObject.Parent = this;
        }
    }

    /// <summary>
    /// Removes a 2D game object from the children collection.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    protected void RemoveGameObject2d(IGameObject2d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject2d> collection)
        {
            if (collection.Remove(gameObject))
            {
                gameObject.Parent = null;
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
    public virtual void Update(GameTime gameTime) { }

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
}
