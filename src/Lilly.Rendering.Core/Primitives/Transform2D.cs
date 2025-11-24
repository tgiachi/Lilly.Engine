using Silk.NET.Maths;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 2D transformation with position, rotation, scale, and bounds.
/// </summary>
public class Transform2D
{
    public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
    public float Rotation { get; set; }
    public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;
    public Vector2D<float> Size { get; set; } = Vector2D<float>.Zero;
}
