using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.Cameras.Base;

/// <summary>
/// Represents a base class for 3D cameras, providing position, rotation, projection, and frustum culling functionality.
/// </summary>
public abstract class Base3dCamera : ICamera3D
{
    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;
    private Vector3 _target = new(0, 0, -1);    // Forward
    private float _fieldOfView = MathF.PI / 4f; // 45 degrees
    private float _aspectRatio;
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000f;

    private Matrix4x4 _view = Matrix4x4.Identity;
    private Matrix4x4 _projection = Matrix4x4.Identity;
    private BoundingFrustum? _frustum;

    private bool _viewDirty = true;
    private bool _projectionDirty = true;

    // IGameObject members
    /// <summary>
    /// Gets or sets the unique identifier of the camera.
    /// </summary>
    public uint Id { get; set; }
    public ushort Order => 0; // Cameras don't have a specific render order

    /// <summary>
    /// Gets or sets a value indicating whether the camera is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the camera.
    /// </summary>
    public string Name { get; set; } = "Camera";

    /// <summary>
    /// Gets or sets the position of the camera in 3D space.
    /// </summary>
    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                _viewDirty = true;
            }
        }
    }

    protected void SetProjectionDirty()
    {
        _projectionDirty = true;
    }

    /// <summary>
    /// Gets or sets the rotation of the camera as a quaternion.
    /// </summary>
    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation != value)
            {
                _rotation = value;
                _viewDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the target point the camera is looking at.
    /// </summary>
    public Vector3 Target
    {
        get => _target;
        set
        {
            if (_target != value)
            {
                _target = value;
                _viewDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets the up vector of the camera.
    /// </summary>
    public Vector3 Up { get; private set; } = new(0, 1, 0);

    /// <summary>
    /// Gets the forward direction vector of the camera.
    /// </summary>
    public Vector3 Forward
    {
        get
        {
            var forward = Vector3.Transform(new Vector3(0, 0, -1), _rotation);

            return Vector3.Normalize(forward);
        }
    }

    /// <summary>
    /// Gets the right direction vector of the camera.
    /// </summary>
    public Vector3 Right
    {
        get
        {
            var right = Vector3.Transform(new Vector3(1, 0, 0), _rotation);

            return Vector3.Normalize(right);
        }
    }

    /// <summary>
    /// Gets or sets the field of view angle in radians.
    /// </summary>
    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            if (MathF.Abs(_fieldOfView - value) > MathF.E)
            {
                _fieldOfView = Math.Clamp(value, 0.01f, MathF.PI - 0.01f);
                _projectionDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the aspect ratio of the camera (width / height).
    /// </summary>
    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            if (MathF.Abs(_aspectRatio - value) > float.Epsilon)
            {
                _aspectRatio = Math.Max(value, 0.01f);
                _projectionDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the distance to the near clipping plane.
    /// </summary>
    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            if (MathF.Abs(_nearPlane - value) > float.Epsilon)
            {
                _nearPlane = Math.Max(value, 0.01f);
                _projectionDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the distance to the far clipping plane.
    /// </summary>
    public float FarPlane
    {
        get => _farPlane;
        set
        {
            if (MathF.Abs(_farPlane - value) > float.Epsilon)
            {
                _farPlane = Math.Max(value, _nearPlane + 0.01f);
                _projectionDirty = true;
            }
        }
    }

    /// <summary>
    /// Gets the view matrix of the camera.
    /// </summary>
    public Matrix4x4 View
    {
        get
        {
            if (_viewDirty)
            {
                UpdateViewMatrix();
                _viewDirty = false;
            }

            return _view;
        }
    }

    /// <summary>
    /// Gets the projection matrix of the camera.
    /// </summary>
    public Matrix4x4 Projection
    {
        get
        {
            if (_projectionDirty)
            {
                UpdateProjectionMatrix();
                _projectionDirty = false;
            }

            return _projection;
        }
    }

    /// <summary>
    /// Gets the bounding frustum of the camera for culling.
    /// </summary>
    public BoundingFrustum Frustum
    {
        get
        {
            if (_frustum == null || _viewDirty || _projectionDirty)
            {
                var viewProjection = View * Projection;
                _frustum = new(viewProjection);
            }

            return _frustum;
        }
    }

    /// <summary>
    /// Gets a ray from the camera through a screen position for picking.
    /// </summary>
    /// <param name="screenPosition">The screen position in pixels.</param>
    /// <param name="viewport">The viewport information.</param>
    /// <returns>The pick ray.</returns>
    public virtual Ray GetPickRay(Vector2 screenPosition, Viewport viewport)
    {
        // Convert screen position to NDC (Normalized Device Coordinates)
        var x = 2.0f * screenPosition.X / viewport.Width - 1.0f;
        var y = 1.0f - 2.0f * screenPosition.Y / viewport.Height;

        // Create points in NDC space
        var nearPointNDC = new Vector3(x, y, 0);
        var farPointNDC = new Vector3(x, y, 1);

        // Get inverse view-projection matrix
        Matrix4x4.Invert(View * Projection, out var invViewProj);

        // Transform to world space
        var nearPoint = Vector3.Transform(nearPointNDC, invViewProj);
        var farPoint = Vector3.Transform(farPointNDC, invViewProj);

        // Calculate direction
        var direction = farPoint - nearPoint;
        direction = Vector3.Normalize(direction);

        return new(nearPoint, direction);
    }

    /// <summary>
    /// Gets or sets the maximum distance for frustum culling.
    /// </summary>
    public float CullingDistance { get; set; } = 200f;

    /// <summary>
    /// Sets the camera to look at a target point with a specified up vector.
    /// </summary>
    /// <param name="target">The target point to look at.</param>
    /// <param name="up">The up vector.</param>
    public virtual void LookAt(Vector3 target, Vector3 up)
    {
        Target = target;
        Up = up;
        _viewDirty = true;
    }

    /// <summary>
    /// Moves the camera by the specified offset.
    /// </summary>
    /// <param name="offset">The offset to move by.</param>
    public virtual void Move(Vector3 offset)
    {
        Position += offset;
    }

    /// <summary>
    /// Moves the camera forward by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to move forward.</param>
    public virtual void MoveForward(float distance)
    {
        Move(Forward * distance);
    }

    /// <summary>
    /// Moves the camera right by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to move right.</param>
    public virtual void MoveRight(float distance)
    {
        Move(Right * distance);
    }

    /// <summary>
    /// Moves the camera up by the specified distance.
    /// </summary>
    /// <param name="distance">The distance to move up.</param>
    public virtual void MoveUp(float distance)
    {
        Move(new Vector3(0, 1, 0) * distance);
    }

    /// <summary>
    /// Determines whether a 3D game object is within the camera's frustum.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>True if the object is in the frustum; otherwise, false.</returns>
    public bool IsInFrustum(IGameObject3d gameObject)
    {
        if (gameObject.IgnoreFrustumCulling)
        {
            return true;
        }

        var toObject = gameObject.Transform.Position - Position;

        if (toObject.LengthSquared() > CullingDistance * CullingDistance)
        {
            return false;
        }

        var boundingBox = gameObject.BoundingBox;

        return Frustum.Intersects(boundingBox.Min, boundingBox.Max);
    }

    /// <summary>
    /// Calculates the distance from the camera to a 3D game object.
    /// </summary>
    /// <param name="gameObject">The game object to measure distance to.</param>
    /// <returns>The distance to the game object.</returns>
    public float Distance(IGameObject3d gameObject)
    {
        var toObject = gameObject.Transform.Position - Position;

        return toObject.Length();
    }

    /// <summary>
    /// Rotates the camera by the specified pitch, yaw, and roll angles.
    /// </summary>
    /// <param name="pitch">The pitch angle in radians.</param>
    /// <param name="yaw">The yaw angle in radians.</param>
    /// <param name="roll">The roll angle in radians.</param>
    public virtual void Rotate(float pitch, float yaw, float roll)
    {
        var pitchRotation = Quaternion.CreateFromAxisAngle(Right, pitch);
        var yawRotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), yaw);
        var rollRotation = Quaternion.CreateFromAxisAngle(Forward, roll);

        Rotation = rollRotation * yawRotation * pitchRotation * Rotation;
    }

    /// <summary>
    /// Updates the camera state based on the elapsed game time.
    /// </summary>
    /// <param name="gameTime">The elapsed game time.</param>
    public abstract void Update(GameTime gameTime);

    protected virtual void UpdateProjectionMatrix()
    {
        _projection = Matrix4x4.CreatePerspectiveFieldOfView(
            _fieldOfView,
            _aspectRatio,
            _nearPlane,
            _farPlane
        );
    }

    protected virtual void UpdateViewMatrix()
    {
        _view = Matrix4x4.CreateLookAt(_position, _target, Up);
    }
}
