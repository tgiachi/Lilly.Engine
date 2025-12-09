using System.Drawing;
using System.Numerics;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.SpriteBatcher;

public interface ILillySpriteBatcher
{
    /// <summary>
    /// Draws a hollow rectangle (border only) using four stretched white pixel textures.
    /// Note: Rotation is not supported for hollow rectangles.
    /// </summary>
    /// <param name="position">Top-left position of the rectangle.</param>
    /// <param name="size">Width and height of the rectangle.</param>
    /// <param name="color">Color to apply to the border.</param>
    /// <param name="thickness">Thickness of the border in pixels (default: 2.0f).</param>
    /// <param name="depth">Depth/Z-index for layering (default: 0).</param>
    void DrawHollowRectangle(
        Vector2 position,
        Vector2 size,
        Color4b color,
        float thickness = 2.0f,
        float depth = 0.0f
    );

    /// <summary>
    /// Draws a filled rectangle using a stretched white pixel texture.
    /// </summary>
    /// <param name="position">Top-left position of the rectangle.</param>
    /// <param name="size">Width and height of the rectangle.</param>
    /// <param name="color">Color to apply to the rectangle.</param>
    /// <param name="rotation">Rotation angle in radians (default: 0).</param>
    /// <param name="origin">Origin point for rotation (default: Vector2.Zero).</param>
    /// <param name="depth">Depth/Z-index for layering (default: 0).</param>
    void DrawRectangle(
        Vector2 position,
        Vector2 size,
        Color4b color,
        float rotation = 0f,
        Vector2? origin = null,
        float depth = 0.0f
    );

    void DrawText(
        string fontName,
        int size,
        string text,
        Vector2 position,
        Color4b color,
        float rotation = 0f,
        Vector2? scale = null
    );

    void DrawTexure(
        string texture,
        Vector2? position = null,
        Rectangle? destination = null,
        Rectangle? source = null,
        Color4b? color = null,
        Vector2? origin = null,
        Vector2? scale = null,
        float? rotation = null,
        float depth = 0.0f
    );
}
