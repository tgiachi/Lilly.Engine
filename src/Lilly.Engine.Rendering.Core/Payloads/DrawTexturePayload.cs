using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload data for texture rendering commands.
/// </summary>
public readonly struct DrawTexturePayload
{
    public string Texture { get; init; }
    public Matrix3X2<float>? Transform { get; init; }
    public Vector2D<float>? Position { get; init; }
    public Rectangle<float>? Destination { get; init; }
    public Rectangle<float>? Source { get; init; }
    public Color4b Color { get; init; }
    public Vector2D<float>? Origin { get; init; }
    public Vector2D<float>? Scale { get; init; }
    public float? Rotation { get; init; }
    public float Depth { get; init; }

    /// <summary>
    /// Initializes a new instance of the DrawTexturePayload struct.
    /// </summary>
    /// <param name="texture">The name of the texture to draw.</param>
    /// <param name="transform">The transformation matrix to apply.</param>
    /// <param name="position">The position to draw the texture at.</param>
    /// <param name="destination">The destination rectangle.</param>
    /// <param name="source">The source rectangle within the texture.</param>
    /// <param name="color">The color tint to apply.</param>
    /// <param name="origin">The origin point for rotation and scaling.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="depth">The depth value for layering.</param>
    public DrawTexturePayload(
        string texture,
        Matrix3X2<float>? transform = null,
        Vector2D<float>? position = null,
        Rectangle<float>? destination = null,
        Rectangle<float>? source = null,
        Color4b? color = null,
        Vector2D<float>? origin = null,
        Vector2D<float>? scale = null,
        float? rotation = null,
        float depth = 0.0f
    )
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
