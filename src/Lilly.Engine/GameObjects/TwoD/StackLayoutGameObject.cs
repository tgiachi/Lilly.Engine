using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.GameObjects.TwoD;

public enum StackOrientation
{
    Horizontal,
    Vertical
}

/// <summary>
/// Simple stack layout that positions 2D children horizontally or vertically with spacing and padding.
/// </summary>
public class StackLayoutGameObject : Base2dGameObject
{
    private StackOrientation _orientation = StackOrientation.Vertical;
    private float _spacing;
    private Vector2 _padding = Vector2.Zero;
    private bool _isLayoutDirty = true;

    public StackOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                _isLayoutDirty = true;
            }
        }
    }

    /// <summary>
    /// Space inserted between items along the stack axis.
    /// </summary>
    public float Spacing
    {
        get => _spacing;
        set
        {
            if (MathF.Abs(_spacing - value) > float.Epsilon)
            {
                _spacing = value;
                _isLayoutDirty = true;
            }
        }
    }

    /// <summary>
    /// Padding applied around the stacked content.
    /// </summary>
    public Vector2 Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = value;
                _isLayoutDirty = true;
            }
        }
    }

    public StackLayoutGameObject(IRenderPipeline gameObjectManager, string name = "StackLayoutGameObject", uint zIndex = 0)
        : base(name, gameObjectManager, zIndex) { }

    /// <summary>
    /// Adds children to the layout and reflows.
    /// </summary>
    public void AddChildren(params IGameObject2d[] gameObjects)
    {
        AddGameObject2d(gameObjects);
        _isLayoutDirty = true;
    }

    /// <summary>
    /// Marks the layout as dirty so it will reflow on the next update (useful if a child size changed).
    /// </summary>
    public void MarkDirty()
        => _isLayoutDirty = true;

    /// <summary>
    /// Removes a child from the layout and reflows.
    /// </summary>
    public void RemoveChild(IGameObject2d gameObject)
    {
        RemoveGameObject2d(gameObject);
        _isLayoutDirty = true;
    }

    public override void Update(GameTime gameTime)
    {
        LayoutIfDirty();
        base.Update(gameTime);
    }

    private static Vector2 GetLocalSize(IGameObject2d gameObject)
    {
        var size = gameObject.Transform.Size;
        var scale = gameObject.Transform.Scale;

        return new(size.X * scale.X, size.Y * scale.Y);
    }

    private void LayoutIfDirty()
    {
        if (!_isLayoutDirty)
        {
            return;
        }

        var children2d = Children.OfType<IGameObject2d>().ToList();
        var count = children2d.Count;

        var cursorX = _padding.X;
        var cursorY = _padding.Y;
        var maxWidth = 0f;
        var maxHeight = 0f;

        foreach (var child in children2d)
        {
            var size = GetLocalSize(child);
            child.Transform.Position = new(cursorX, cursorY);

            maxWidth = MathF.Max(maxWidth, size.X);
            maxHeight = MathF.Max(maxHeight, size.Y);

            if (_orientation == StackOrientation.Vertical)
            {
                cursorY += size.Y + _spacing;
            }
            else
            {
                cursorX += size.X + _spacing;
            }
        }

        float contentWidth;
        float contentHeight;

        if (_orientation == StackOrientation.Vertical)
        {
            contentWidth = maxWidth;
            contentHeight = cursorY - _padding.Y - (count > 0 ? _spacing : 0);
        }
        else
        {
            contentWidth = cursorX - _padding.X - (count > 0 ? _spacing : 0);
            contentHeight = maxHeight;
        }

        Transform.Size = new(
            contentWidth + _padding.X * 2,
            contentHeight + _padding.Y * 2
        );

        _isLayoutDirty = false;
    }
}
