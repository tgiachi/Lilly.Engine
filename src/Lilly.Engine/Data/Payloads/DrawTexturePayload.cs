using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Data.Payloads;

/// <summary>
/// Payload data for texture/sprite rendering commands.
/// </summary>
public readonly struct DrawTexturePayload
{
    /// <summary>
    /// Gets the texture to render.
    /// </summary>
    public Texture2D Texture { get; init; }

    /// <summary>
    /// Gets the transformation matrix to apply.
    /// </summary>
    public Matrix3X2<float>? Transform { get; init; }

    /// <summary>
    /// Gets the position where the texture will be rendered.
    /// </summary>
    public Vector2D<float>? Position { get; init; }

    /// <summary>
    /// Gets the destination rectangle on screen.
    /// </summary>
    public Rectangle<float>? Destination { get; init; }

    /// <summary>
    /// Gets the source rectangle from the texture.
    /// </summary>
    public Rectangle<float>? Source { get; init; }

    /// <summary>
    /// Gets the tint color to apply to the texture.
    /// </summary>
    public Color4b Color { get; init; }

    /// <summary>
    /// Gets the origin point for rotation and scaling.
    /// </summary>
    public Vector2D<float>? Origin { get; init; }

    /// <summary>
    /// Gets the scale factor to apply.
    /// </summary>
    public Vector2D<float>? Scale { get; init; }

    /// <summary>
    /// Gets the rotation angle in radians.
    /// </summary>
    public float? Rotation { get; init; }

    /// <summary>
    /// Gets the depth value for layer sorting.
    /// </summary>
    public float Depth { get; init; }

    /// <summary>
    /// Initializes a new instance of the DrawTexturePayload struct.
    /// </summary>
    /// <param name="texture">The texture to render.</param>
    /// <param name="transform">Optional transformation matrix.</param>
    /// <param name="position">Optional position.</param>
    /// <param name="destination">Optional destination rectangle.</param>
    /// <param name="source">Optional source rectangle.</param>
    /// <param name="color">Optional tint color (defaults to white).</param>
    /// <param name="origin">Optional origin point.</param>
    /// <param name="scale">Optional scale factor.</param>
    /// <param name="rotation">Optional rotation in radians.</param>
    /// <param name="depth">Optional depth value for sorting (defaults to 0).</param>
    public DrawTexturePayload(
        Texture2D texture,
        Matrix3X2<float>? transform = null,
        Vector2D<float>? position = null,
        Rectangle<float>? destination = null,
        Rectangle<float>? source = null,
        Color4b? color = null,
        Vector2D<float>? origin = null,
        Vector2D<float>? scale = null,
        float? rotation = null,
        float depth = 0.0f)
    {
        Texture = texture;
        Transform = transform;
        Position = position;
        Destination = destination;
        Source = source;
        Color = color ?? Color4b.White;
        Origin = origin;
        Scale = scale;
        Rotation = rotation;
        Depth = depth;
    }
}
