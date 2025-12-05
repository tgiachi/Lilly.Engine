using System;
using System.Numerics;

namespace Lilly.Rendering.Core.Primitives;

/// <summary>
/// Represents a 2D transformation with position, rotation, scale, and bounds.
/// </summary>
public class Transform2D
{
    private Vector2 _position = Vector2.Zero;
    private float _rotation;
    private Vector2 _scale = Vector2.One;
    private Vector2 _size = Vector2.Zero;

    /// <summary>
    /// Raised whenever any component of the transform changes.
    /// </summary>
    public event Action<Transform2D>? Changed;

    /// <summary>
    /// Gets or sets the position of the transform in 2D space.
    /// </summary>
    public Vector2 Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Gets or sets the rotation angle in radians.
    /// </summary>
    public float Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation.Equals(value)) return;
            _rotation = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Gets or sets the scale factors for width and height.
    /// </summary>
    public Vector2 Scale
    {
        get => _scale;
        set
        {
            if (_scale == value) return;
            _scale = value;
            OnChanged();
        }
    }

    /// <summary>
    /// Gets or sets the size (bounds) of the transformed object.
    /// </summary>
    public Vector2 Size
    {
        get => _size;
        set
        {
            if (_size == value) return;
            _size = value;
            OnChanged();
        }
    }

    private void OnChanged() => Changed?.Invoke(this);
}
