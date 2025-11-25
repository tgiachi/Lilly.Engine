using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 2D transformation with position, rotation, scale, and bounds.
/// </summary>
public class Transform2D
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Rotation { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;
    public Vector2 Size { get; set; } = Vector2.Zero;
}
