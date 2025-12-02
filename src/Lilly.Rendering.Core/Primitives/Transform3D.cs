using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 3D transformation with position, rotation, and scale.
/// </summary>
public class Transform3D
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
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
