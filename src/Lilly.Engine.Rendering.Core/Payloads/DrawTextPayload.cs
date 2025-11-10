using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload data for text rendering commands.
/// </summary>
public readonly struct DrawTextPayload
{
    /// <summary>
    /// Gets the font family name to use for rendering.
    /// </summary>
    public string FontFamily { get; init; }

    /// <summary>
    /// Gets the text content to render.
    /// </summary>
    public string Text { get; init; }

    /// <summary>
    /// Gets the font size in pixels.
    /// </summary>
    public int FontSize { get; init; }

    /// <summary>
    /// Gets the position where the text will be rendered.
    /// </summary>
    public Vector2D<float> Position { get; init; }

    /// <summary>
    /// Gets the scale factor for the text.
    /// </summary>
    public Vector2D<float> Scale { get; init; }

    /// <summary>
    /// Gets the rotation angle in radians.
    /// </summary>
    public float Rotation { get; init; }

    /// <summary>
    /// Gets the text color.
    /// </summary>
    public Color4b Color { get; init; }

    /// <summary>
    /// Gets the depth value for layering.
    /// </summary>
    public float Depth { get; init; }

    /// <summary>
    /// Initializes a new instance of the DrawTextPayload struct.
    /// </summary>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    /// <param name="position">The position to render at.</param>
    /// <param name="scale">Optional scale factor (defaults to 1,1).</param>
    /// <param name="rotation">Optional rotation in radians (defaults to 0).</param>
    /// <param name="color">Optional text color (defaults to white).</param>
    /// <param name="depth">Optional depth value for layering (defaults to 0).</param>
    public DrawTextPayload(
        string fontFamily,
        string text,
        int fontSize,
        Vector2D<float> position,
        Vector2D<float>? scale = null,
        float rotation = 0f,
        Color4b? color = null,
        float depth = 0f
    )
    {
        color ??= Color4b.White;
        scale ??= new(1f, 1f);

        FontFamily = fontFamily;
        Text = text;
        FontSize = fontSize;
        Position = position;
        Scale = scale.Value;
        Rotation = rotation;
        Color = color.Value;
        Depth = depth;
    }
}
