using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Primitives;

/// <summary>
/// Represents a 2D transformation with position, rotation, and scale.
/// </summary>
public class Transform2D
{
    public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
    public float Rotation { get; set; } = 0f;
    public Vector2D<float> Scale { get; set; } = Vector2D<float>.One;

    public Vector2D<int> Size { get; set; }

}
