using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 3D transformation with position, rotation, and scale.
/// </summary>
public class Transform3D
{
    /// <summary>
    /// Gets or sets the position of the transform in 3D space.
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets the rotation represented as a quaternion.
    /// </summary>
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    /// <summary>
    /// Gets or sets the scale factors for x, y, and z axes.
    /// </summary>
    public Vector3 Scale { get; set; } = Vector3.One;

    /// <summary>
    /// Gets the transformation matrix combining translation, rotation, and scaling.
    /// </summary>
    /// <returns>The combined transformation matrix.</returns>
    public Matrix4x4 GetTransformationMatrix()
    {
        var translation = Matrix4x4.CreateTranslation(Position);
        var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
        var scaling = Matrix4x4.CreateScale(Scale);

        return scaling * rotation * translation;
    }

    public override string ToString()
    {
        return $"Position: {Position}, Rotation: {Rotation}, Scale: {Scale}";
    }
}
