using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Primitives;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Cameras.Base;

/// <summary>
/// Base implementation of a 3D camera with position, rotation, and projection.
/// </summary>
public abstract class Base3dCamera : ICamera3D
{
    private const float Epsilon = 1e-6f; // Epsilon per confronti float

    private Vector3D<float> _position = Vector3D<float>.Zero;
    private Quaternion<float> _rotation = Quaternion<float>.Identity;
    private Vector3D<float> _target = new(0, 0, 1); // Forward
    private float _fieldOfView = MathF.PI / 4f;     // 45 degrees
    private float _aspectRatio;
    private float _nearPlane = 0.1f;
    private float _farPlane = 1000f;

    private Matrix4X4<float> _view = Matrix4X4<float>.Identity;
    private Matrix4X4<float> _projection = Matrix4X4<float>.Identity;
    private BoundingFrustum? _frustum;

    private bool _viewDirty = true;
    private bool _projectionDirty = true;

    // IGameObject members
    public IGameObject? Parent { get; set; }
    public GameObjectCollection<IGameObject> Children { get; } = new();
    public uint Id { get; set; }
    public ushort Order => 0; // Cameras don't have a specific render order

    public bool Enabled { get; set; } = true;

    public string Name { get; set; } = "Camera";

    public Vector3D<float> Position
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

    public Quaternion<float> Rotation
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

    public Vector3D<float> Target
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

    public Vector3D<float> Up { get; private set; } = new(0, 1, 0); // Vector3D.Up

    public Vector3D<float> Forward
    {
        get
        {
            var forward = Vector3D.Transform(new(0, 0, 1), _rotation);

            return Vector3D.Normalize(forward);
        }
    }

    public Vector3D<float> Right
    {
        get
        {
            var right = Vector3D.Transform(new(1, 0, 0), _rotation);

            return Vector3D.Normalize(right);
        }
    }

    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            if (MathF.Abs(_fieldOfView - value) > Epsilon)
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
            if (MathF.Abs(_aspectRatio - value) > Epsilon)
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
            if (MathF.Abs(_nearPlane - value) > Epsilon)
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
            if (MathF.Abs(_farPlane - value) > Epsilon)
            {
                _farPlane = Math.Max(value, _nearPlane + 0.01f);
                _projectionDirty = true;
            }
        }
    }

    public Matrix4X4<float> View
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

    public Matrix4X4<float> Projection
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

    public virtual Ray GetPickRay(Vector2D<int> screenPosition, Viewport viewport)
    {
        // Convert screen position to NDC (Normalized Device Coordinates)
        var x = 2.0f * screenPosition.X / viewport.Width - 1.0f;
        var y = 1.0f - 2.0f * screenPosition.Y / viewport.Height;

        // Create points in NDC space
        var nearPointNDC = new Vector3D<float>(x, y, 0);
        var farPointNDC = new Vector3D<float>(x, y, 1);

        // Get inverse view-projection matrix
        Matrix4X4.Invert(View * Projection, out var invViewProj);

        // Transform to world space
        var nearPoint = Vector3D.Transform(nearPointNDC, invViewProj);
        var farPoint = Vector3D.Transform(farPointNDC, invViewProj);

        // Calculate direction
        var direction = farPoint - nearPoint;
        direction = Vector3D.Normalize(direction);

        return new(nearPoint, direction);
    }

    public virtual void LookAt(Vector3D<float> target, Vector3D<float> up)
    {
        Target = target;
        Up = up;
        _viewDirty = true;
    }

    public void LookAt(IGameObject3D targetObject)
    {
        LookAt(targetObject.Transform.Position, new(0, 1, 0));
    }

    public virtual void Move(Vector3D<float> offset)
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
        Move(new Vector3D<float>(0, 1, 0) * distance);
    }

    /// <summary>
    /// Cameras don't render anything themselves, they define the view for rendering.
    /// </summary>
    public virtual IEnumerable<RenderCommand> Render(GameTime gameTime)
        => [];

    public virtual void Rotate(float pitch, float yaw, float roll)
    {
        var pitchRotation = Quaternion<float>.CreateFromAxisAngle(Right, pitch);
        var yawRotation = Quaternion<float>.CreateFromAxisAngle(new(0, 1, 0), yaw);
        var rollRotation = Quaternion<float>.CreateFromAxisAngle(Forward, roll);

        Rotation = rollRotation * yawRotation * pitchRotation * Rotation;
    }

    public abstract void Update(GameTime gameTime);

    protected virtual void UpdateProjectionMatrix()
    {
        _projection = Matrix4X4.CreatePerspectiveFieldOfView(
            _fieldOfView,
            _aspectRatio,
            _nearPlane,
            _farPlane
        );
    }

    protected virtual void UpdateViewMatrix()
    {
        _view = Matrix4X4.CreateLookAt(_position, _target, Up);
    }
}
