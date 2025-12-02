using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.Cameras.Base;

/// <summary>
/// Base implementation of a 3D camera with position, rotation, and projection.
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
    public uint Id { get; set; }
    public ushort Order => 0; // Cameras don't have a specific render order

    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "Camera";

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

    public Vector3 Up { get; private set; } = new(0, 1, 0);

    public Vector3 Forward
    {
        get
        {
            var forward = Vector3.Transform(new Vector3(0, 0, -1), _rotation);

            return Vector3.Normalize(forward);
        }
    }

    public Vector3 Right
    {
        get
        {
            var right = Vector3.Transform(new Vector3(1, 0, 0), _rotation);

            return Vector3.Normalize(right);
        }
    }

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
        protected set;
    }

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

    public float CullingDistance { get; set; } = 200f;

    public virtual void LookAt(Vector3 target, Vector3 up)
    {
        Target = target;
        Up = up;
        _viewDirty = true;
    }

    public virtual void Move(Vector3 offset)
    {
        Position += offset;
    }

    public virtual void MoveForward(float distance)
    {
        Move(Forward * distance);
    }

    public virtual void MoveRight(float distance)
    {
        Move(Right * distance);
    }

    public virtual void MoveUp(float distance)
    {
        Move(new Vector3(0, 1, 0) * distance);
    }

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

    public float Distance(IGameObject3d gameObject)
    {
        var toObject = gameObject.Transform.Position - Position;

        return toObject.Length();
    }

    public virtual void Rotate(float pitch, float yaw, float roll)
    {
        var pitchRotation = Quaternion.CreateFromAxisAngle(Right, pitch);
        var yawRotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), yaw);
        var rollRotation = Quaternion.CreateFromAxisAngle(Forward, roll);

        Rotation = rollRotation * yawRotation * pitchRotation * Rotation;
    }

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
