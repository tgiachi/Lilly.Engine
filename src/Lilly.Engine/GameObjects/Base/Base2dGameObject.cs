using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Collections;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Engine.GameObjects.Base;

public abstract class Base2dGameObject : IGameObject2d, IUpdateble
{
    public uint Id { get; set; }
    public string Name { get; set; }
    public uint ZIndex { get; set; }
    public bool IsActive { get; set; }

    public IGameObject? Parent { get; set; }
    public IEnumerable<IGameObject> Children { get; } = new GameObjectCollection<IGameObject2d>();

    protected ILillySpriteBatcher? SpriteBatcher { get; private set; }

    public Transform2D Transform { get; } = new Transform2D();

    protected Base2dGameObject(string name, uint zIndex = 0)
    {
        Name = name;
        ZIndex = zIndex;
        IsActive = true;
    }

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

    protected void AddGameObject2d(IGameObject2d gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (Children is GameObjectCollection<IGameObject2d> collection)
        {
            collection.Add(gameObject);
            gameObject.Parent = this;
        }
    }

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

    protected virtual void OnDraw(GameTime gameTime) { }

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
