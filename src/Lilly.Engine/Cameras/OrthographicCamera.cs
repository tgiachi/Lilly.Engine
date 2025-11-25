using System.Numerics;
using Lilly.Engine.Cameras.Base;
using Lilly.Engine.Core.Data.Privimitives;
using TrippyGL;

namespace Lilly.Engine.Cameras;

/// <summary>
/// An orthographic camera that renders objects without perspective distortion.
/// All objects maintain the same size regardless of distance from the camera.
/// Ideal for 2D games, strategy games, level editors, and UI.
/// </summary>
public class OrthographicCamera : Base3dCamera
{
    private const float Epsilon = 1e-6f;

    private float _orthoWidth = 10f;
    private float _orthoHeight = 10f;
    private float _zoom = 1f;
    private float _targetZoom = 1f;
    private float _zoomSpeed = 5f;
    private float _movementSpeed = 10f;

    private Vector3 _minBounds;
    private Vector3 _maxBounds;

    /// <summary>
    /// Gets or sets the orthographic viewport width in world units.
    /// </summary>
    public float OrthoWidth
    {
        get => _orthoWidth;
        set
        {
            if (MathF.Abs(_orthoWidth - value) > Epsilon)
            {
                _orthoWidth = Math.Max(value, 0.1f);
                MarkProjectionDirty();
            }
        }
    }

    /// <summary>
    /// Gets or sets the orthographic viewport height in world units.
    /// </summary>
    public float OrthoHeight
    {
        get => _orthoHeight;
        set
        {
            if (MathF.Abs(_orthoHeight - value) > Epsilon)
            {
                _orthoHeight = Math.Max(value, 0.1f);
                MarkProjectionDirty();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current zoom level (1.0 = normal, 2.0 = 2x zoom in, 0.5 = zoom out).
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set
        {
            var newZoom = Math.Clamp(value, MinZoom, MaxZoom);

            if (MathF.Abs(_zoom - newZoom) > Epsilon)
            {
                _zoom = newZoom;
                _targetZoom = newZoom;
                MarkProjectionDirty();
            }
        }
    }

    /// <summary>
    /// Gets or sets the minimum allowed zoom level.
    /// </summary>
    public float MinZoom { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets the maximum allowed zoom level.
    /// </summary>
    public float MaxZoom { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the speed of smooth zoom transitions.
    /// </summary>
    public float ZoomSpeed
    {
        get => _zoomSpeed;
        set => _zoomSpeed = Math.Max(value, 0.1f);
    }

    /// <summary>
    /// Gets or sets the camera movement speed.
    /// </summary>
    public float MovementSpeed
    {
        get => _movementSpeed;
        set => _movementSpeed = Math.Max(value, 0.1f);
    }

    /// <summary>
    /// Gets or sets the minimum bounds for camera position constraints.
    /// Only applied if EnableBoundsConstraints is true.
    /// </summary>
    public Vector3 MinBounds
    {
        get => _minBounds;
        set => _minBounds = value;
    }

    /// <summary>
    /// Gets or sets the maximum bounds for camera position constraints.
    /// Only applied if EnableBoundsConstraints is true.
    /// </summary>
    public Vector3 MaxBounds
    {
        get => _maxBounds;
        set => _maxBounds = value;
    }

    /// <summary>
    /// Gets or sets whether to enable bounds constraints for camera movement.
    /// </summary>
    public bool EnableBoundsConstraints { get; set; }

    /// <summary>
    /// Initializes a new orthographic camera with default settings.
    /// </summary>
    /// <param name="name">The name of the camera</param>
    public OrthographicCamera(string name = "OrthographicCamera")
    {
        Name = name;
        Position = new Vector3(0, 0, -10);
        Target = Vector3.Zero;
    }

    /// <summary>
    /// Clears the bounds constraints, allowing unrestricted camera movement.
    /// </summary>
    public void ClearBounds()
    {
        EnableBoundsConstraints = false;
    }

    /// <summary>
    /// Moves the camera in a 2D plane (useful for pan controls).
    /// </summary>
    /// <param name="deltaX">Horizontal movement in world units</param>
    /// <param name="deltaY">Vertical movement in world units</param>
    public void Pan(float deltaX, float deltaY)
    {
        var offset = new Vector3(deltaX, deltaY, 0);
        Position += offset;
        Target += offset;
        ApplyBoundsConstraints();
    }

    /// <summary>
    /// Converts a screen position to world coordinates.
    /// </summary>
    /// <param name="screenPoint">Screen position in pixels</param>
    /// <param name="viewport">The viewport</param>
    /// <returns>World position</returns>
    public Vector3 ScreenToWorld(Vector2 screenPoint, Viewport viewport)
    {
        // Normalize screen coordinates to [-1, 1]
        var x = 2.0f * screenPoint.X / viewport.Width - 1.0f;
        var y = 1.0f - 2.0f * screenPoint.Y / viewport.Height;

        // In orthographic projection, we can directly calculate the world position
        var effectiveWidth = _orthoWidth / _zoom;
        var effectiveHeight = _orthoHeight / _zoom;

        var worldX = Position.X + x * effectiveWidth * 0.5f;
        var worldY = Position.Y + y * effectiveHeight * 0.5f;

        return new Vector3(worldX, worldY, Position.Z);
    }

    /// <summary>
    /// Sets the bounds constraints for camera movement.
    /// </summary>
    /// <param name="min">Minimum position bounds</param>
    /// <param name="max">Maximum position bounds</param>
    public void SetBounds(Vector3 min, Vector3 max)
    {
        _minBounds = min;
        _maxBounds = max;
        EnableBoundsConstraints = true;
        ApplyBoundsConstraints();
    }

    /// <summary>
    /// Sets the orthographic viewport size in world units.
    /// </summary>
    /// <param name="width">Width in world units</param>
    /// <param name="height">Height in world units</param>
    public void SetSize(float width, float height)
    {
        _orthoWidth = Math.Max(width, 0.1f);
        _orthoHeight = Math.Max(height, 0.1f);
        MarkProjectionDirty();
    }

    /// <summary>
    /// Performs smooth zoom towards the target zoom level.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public void SmoothZoom(float deltaTime)
    {
        if (MathF.Abs(_zoom - _targetZoom) > Epsilon)
        {
            var lerpFactor = 1f - MathF.Exp(-_zoomSpeed * deltaTime);
            lerpFactor = Math.Clamp(lerpFactor, 0f, 1f);

            var oldZoom = _zoom;
            _zoom = _zoom + (_targetZoom - _zoom) * lerpFactor;

            if (MathF.Abs(_zoom - oldZoom) > Epsilon)
            {
                MarkProjectionDirty();
            }
        }
    }

    /// <summary>
    /// Updates the camera, applying smooth zoom and other effects.
    /// </summary>
    /// <param name="gameTime">Game timing information</param>
    public override void Update(GameTime gameTime)
    {
        var deltaTime = gameTime.GetElapsedSeconds();
        SmoothZoom(deltaTime);
        ApplyBoundsConstraints();
    }

    /// <summary>
    /// Converts a world position to screen coordinates.
    /// </summary>
    /// <param name="worldPoint">World position</param>
    /// <param name="viewport">The viewport</param>
    /// <returns>Screen position in pixels</returns>
    public Vector2 WorldToScreen(Vector3 worldPoint, Viewport viewport)
    {
        var effectiveWidth = _orthoWidth / _zoom;
        var effectiveHeight = _orthoHeight / _zoom;

        var relativeX = (worldPoint.X - Position.X) / (effectiveWidth * 0.5f);
        var relativeY = (worldPoint.Y - Position.Y) / (effectiveHeight * 0.5f);

        var screenX = (relativeX + 1.0f) * 0.5f * viewport.Width;
        var screenY = (1.0f - relativeY) * 0.5f * viewport.Height;

        return new Vector2(screenX, screenY);
    }

    /// <summary>
    /// Zooms in or out centered at a specific screen point (useful for mouse wheel zoom).
    /// </summary>
    /// <param name="screenPoint">The screen position to zoom towards (in pixels)</param>
    /// <param name="viewport">The viewport information</param>
    /// <param name="zoomAmount">Amount to zoom (positive = zoom in, negative = zoom out)</param>
    public void ZoomAtScreenPoint(Vector2 screenPoint, Viewport viewport, float zoomAmount)
    {
        // Get world position before zoom
        var worldPosBefore = ScreenToWorld(screenPoint, viewport);

        // Apply zoom
        var newZoom = Math.Clamp(_zoom + zoomAmount, MinZoom, MaxZoom);

        if (MathF.Abs(_zoom - newZoom) > Epsilon)
        {
            _zoom = newZoom;
            _targetZoom = newZoom;
            MarkProjectionDirty();

            // Get world position after zoom
            var worldPosAfter = ScreenToWorld(screenPoint, viewport);

            // Offset camera to keep the same world point under the cursor
            var offset = worldPosBefore - worldPosAfter;
            Position += offset;
            Target += offset;

            ApplyBoundsConstraints();
        }
    }

    /// <summary>
    /// Zooms in by the specified amount.
    /// </summary>
    /// <param name="amount">Amount to zoom in (positive values zoom in)</param>
    public void ZoomIn(float amount)
    {
        _targetZoom = Math.Clamp(_targetZoom + amount, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Zooms out by the specified amount.
    /// </summary>
    /// <param name="amount">Amount to zoom out (positive values zoom out)</param>
    public void ZoomOut(float amount)
    {
        _targetZoom = Math.Clamp(_targetZoom - amount, MinZoom, MaxZoom);
    }

    /// <summary>
    /// Overrides the base projection matrix to use orthographic projection.
    /// </summary>
    protected override void UpdateProjectionMatrix()
    {
        var effectiveWidth = _orthoWidth / _zoom;
        var effectiveHeight = _orthoHeight / _zoom;

        _projection = Matrix4x4.CreateOrthographic(
            effectiveWidth,
            effectiveHeight,
            NearPlane,
            FarPlane
        );
    }

    /// <summary>
    /// Applies bounds constraints to the camera position if enabled.
    /// </summary>
    private void ApplyBoundsConstraints()
    {
        if (!EnableBoundsConstraints)
        {
            return;
        }

        var pos = Position;
        pos.X = Math.Clamp(pos.X, _minBounds.X, _maxBounds.X);
        pos.Y = Math.Clamp(pos.Y, _minBounds.Y, _maxBounds.Y);
        pos.Z = Math.Clamp(pos.Z, _minBounds.Z, _maxBounds.Z);

        if (pos != Position)
        {
            Position = pos;
            Target = new Vector3(pos.X, pos.Y, 0);
        }
    }

    /// <summary>
    /// Marks the projection matrix as dirty to force recalculation.
    /// </summary>
    private void MarkProjectionDirty()
    {
        _projectionDirty = true;
    }

    /// <summary>
    /// Creates an orthographic camera configured for 2D top-down view.
    /// </summary>
    /// <param name="width">Viewport width in world units</param>
    /// <param name="height">Viewport height in world units</param>
    /// <returns>Configured orthographic camera</returns>
    public static OrthographicCamera Create2DTopDown(float width, float height)
    {
        var camera = new OrthographicCamera("TopDownCamera")
        {
            OrthoWidth = width,
            OrthoHeight = height,
            Position = new Vector3(0, 0, -10),
            Target = Vector3.Zero
        };

        camera.LookAt(Vector3.Zero, new Vector3(0, 1, 0));

        return camera;
    }

    /// <summary>
    /// Creates an orthographic camera configured for 2D side-scrolling view.
    /// </summary>
    /// <param name="width">Viewport width in world units</param>
    /// <param name="height">Viewport height in world units</param>
    /// <returns>Configured orthographic camera</returns>
    public static OrthographicCamera Create2DSideScroll(float width, float height)
    {
        var camera = new OrthographicCamera("SideScrollCamera")
        {
            OrthoWidth = width,
            OrthoHeight = height,
            Position = new Vector3(0, 0, -10),
            Target = Vector3.Zero
        };

        camera.LookAt(Vector3.Zero, new Vector3(0, 1, 0));

        return camera;
    }

    /// <summary>
    /// Creates an orthographic camera configured for isometric view.
    /// Typical isometric angle is ~35.264 degrees from horizontal.
    /// </summary>
    /// <param name="width">Viewport width in world units</param>
    /// <param name="height">Viewport height in world units</param>
    /// <returns>Configured orthographic camera</returns>
    public static OrthographicCamera CreateIsometric(float width = 20f, float height = 20f)
    {
        var camera = new OrthographicCamera("IsometricCamera")
        {
            OrthoWidth = width,
            OrthoHeight = height
        };

        // Standard isometric angles: 45 degrees rotation, ~35.264 degrees pitch
        var distance = 20f;
        var angle = MathF.PI / 4f;                   // 45 degrees
        var pitch = MathF.Atan(1f / MathF.Sqrt(2f)); // ~35.264 degrees

        var x = distance * MathF.Sin(angle) * MathF.Cos(pitch);
        var y = distance * MathF.Sin(pitch);
        var z = distance * MathF.Cos(angle) * MathF.Cos(pitch);

        camera.Position = new Vector3(x, y, z);
        camera.LookAt(Vector3.Zero, new Vector3(0, 1, 0));

        return camera;
    }

    /// <summary>
    /// Creates an orthographic camera for UI or HUD rendering.
    /// </summary>
    /// <param name="screenWidth">Screen width in pixels</param>
    /// <param name="screenHeight">Screen height in pixels</param>
    /// <returns>Configured orthographic camera</returns>
    public static OrthographicCamera CreateUI(float screenWidth, float screenHeight)
    {
        var camera = new OrthographicCamera("UICamera")
        {
            OrthoWidth = screenWidth,
            OrthoHeight = screenHeight,
            Position = new Vector3(screenWidth / 2f, screenHeight / 2f, -10),
            Target = new Vector3(screenWidth / 2f, screenHeight / 2f, 0),
            NearPlane = 0.1f,
            FarPlane = 100f
        };

        return camera;
    }

    /// <summary>
    /// Creates an orthographic camera for a minimap view.
    /// </summary>
    /// <param name="worldWidth">Width of the world area to show</param>
    /// <param name="worldHeight">Height of the world area to show</param>
    /// <param name="height">Height of the camera above the world</param>
    /// <returns>Configured orthographic camera</returns>
    public static OrthographicCamera CreateMinimap(float worldWidth, float worldHeight, float height = 50f)
    {
        var camera = new OrthographicCamera("MinimapCamera")
        {
            OrthoWidth = worldWidth,
            OrthoHeight = worldHeight,
            Position = new Vector3(0, height, 0),
            Target = Vector3.Zero
        };

        camera.LookAt(Vector3.Zero, new Vector3(0, 0, 1));

        return camera;
    }

}
