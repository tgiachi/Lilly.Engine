using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// A game object that draws a filled rectangle or a hollow rectangle (border).
/// Uses DefaultTextures.WhiteTextureKey by default.
/// </summary>
public class RectangleGameObject : BaseGameObject2D
{
    private Vector2D<float> _size = new(100, 100);

    /// <summary>
    /// The size of the rectangle.
    /// </summary>
    public Vector2D<float> Size
    {
        get => _size;
        set
        {
            _size = value;
            Transform.Size = value;
        }
    }

    /// <summary>
    /// The color of the rectangle.
    /// </summary>
    public Color4b Color { get; set; } = Color4b.White;

    /// <summary>
    /// If true, draws only the border. If false, draws a filled rectangle.
    /// </summary>
    public bool IsHollow { get; set; } = false;

    /// <summary>
    /// The thickness of the border (only used when IsHollow is true).
    /// </summary>
    public float BorderThickness { get; set; } = 2.0f;

    /// <summary>
    /// Optional custom white pixel texture. If null, uses DefaultTextures.WhiteTextureKey.
    /// </summary>
    public string? CustomWhitePixelTexture { get; set; } = null;

    public RectangleGameObject()
        => Transform.Size = _size;

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (IsHollow)
        {
            // Draw hollow rectangle (border only) - uses Order for depth
            foreach (var command in DrawHollowRectangle(
                         Transform.Position,
                         Size,
                         Color,
                         BorderThickness,
                         whitePixelTexture: CustomWhitePixelTexture
                     ))
            {
                yield return command;
            }
        }
        else
        {
            // Draw filled rectangle - uses Order for depth
            yield return DrawRectangle(
                new(Transform.Position, Size),
                Color,
                whitePixelTexture: CustomWhitePixelTexture
            );
        }
    }
}
