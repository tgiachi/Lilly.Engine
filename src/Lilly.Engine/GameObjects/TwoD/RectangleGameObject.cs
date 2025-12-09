using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.TwoD;

/// <summary>
/// A game object that draws a filled rectangle or a hollow rectangle (border).
/// Uses the white pixel texture for rendering.
/// </summary>
public class RectangleGameObject : Base2dGameObject
{
    private Vector2 _size = new(100, 100);

    /// <summary>
    /// Gets or sets the size of the rectangle.
    /// </summary>
    public Vector2 Size
    {
        get => _size;
        set
        {
            _size = value;
            Transform.Size = value;
        }
    }

    /// <summary>
    /// Gets or sets the color of the rectangle.
    /// </summary>
    public Color4b Color { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets whether to draw only the border (true) or a filled rectangle (false).
    /// </summary>
    public bool IsHollow { get; set; }

    /// <summary>
    /// Gets or sets the thickness of the border (only used when IsHollow is true).
    /// Recommended range: 1.0f - 10.0f.
    /// </summary>
    public float BorderThickness { get; set; } = 2.0f;

    /// <summary>
    /// Initializes a new instance of the <see cref="RectangleGameObject" /> class.
    /// </summary>
    /// <param name="name">The name of the game object (default: "RectangleGameObject").</param>
    /// <param name="zIndex">The rendering z-index (default: 0).</param>
    public RectangleGameObject(IRenderPipeline gameObjectManager, string name = "RectangleGameObject", uint zIndex = 0)
        : base(name, gameObjectManager, zIndex)
        => Transform.Size = _size;

    /// <summary>
    /// Draws the rectangle using the SpriteBatcher.
    /// Filled rectangles support rotation via Transform.Rotation.
    /// Hollow rectangles do not support rotation.
    /// Uses world transforms for hierarchical positioning.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    protected override void OnDraw(GameTime gameTime)
    {
        if (SpriteBatcher == null)
        {
            return;
        }

        var worldPosition = GetWorldPosition();
        var worldRotation = GetWorldRotation();
        var worldScale = GetWorldScale();
        var worldSize = Size * worldScale;

        if (IsHollow)
        {
            // Draw hollow rectangle (border only) - rotation not supported
            SpriteBatcher.DrawHollowRectangle(
                worldPosition,
                worldSize,
                Color,
                BorderThickness,
                ZIndex
            );
        }
        else
        {
            // Draw filled rectangle - supports rotation
            SpriteBatcher.DrawRectangle(
                worldPosition,
                worldSize,
                Color,
                worldRotation,
                Vector2.Zero,
                ZIndex
            );
        }
    }
}
