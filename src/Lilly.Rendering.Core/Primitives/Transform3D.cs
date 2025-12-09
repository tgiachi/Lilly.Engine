using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 3D transformation with position, rotation, and scale.
/// </summary>
public class Transform3D
{
    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;
    private Vector3 _scale = Vector3.One;

    /// <summary>
    /// Raised whenever any component of the transform changes.
    /// </summary>
    public event Action<Transform3D>? Changed;

    /// <summary>
    /// Gets or sets the position of the transform in 3D space.
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }
            _position = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Gets or sets the rotation represented as a quaternion.
    /// </summary>
    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation == value)
            {
                return;
            }
            _rotation = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Gets or sets the scale factors for x, y, and z axes.
    /// </summary>
    public Vector3 Scale
    {
        get => _scale;
        set
        {
            if (_scale == value)
            {
                return;
            }
            _scale = value;
            OnChanged();
        }
    }

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
        => $"Position: {Position}, Rotation: {Rotation}, Scale: {Scale}";

    private void OnChanged()
        => Changed?.Invoke(this);
}
