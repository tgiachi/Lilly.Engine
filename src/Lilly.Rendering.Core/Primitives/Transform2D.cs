using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 2D transformation with position, rotation, scale, and bounds.
/// </summary>
public class Transform2D
{
    /// <summary>
    /// Gets or sets the position of the transform in 2D space.
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the rotation angle in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// Gets or sets the scale factors for width and height.
    /// </summary>
    public Vector2 Scale { get; set; } = Vector2.One;

    /// <summary>
    /// Gets or sets the size (bounds) of the transformed object.
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.Zero;
}
