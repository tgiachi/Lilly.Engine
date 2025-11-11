using Lilly.Engine.Cameras.Base;
using Lilly.Engine.Core.Data.Privimitives;
using Silk.NET.Maths;

namespace Lilly.Engine.Cameras;

/// <summary>
/// A camera that follows a target position with a fixed offset.
/// Useful for third-person cameras following a player character.
/// </summary>
public class FollowCamera : Base3dCamera
{
    private const float Epsilon = 1e-6f;
    private Vector3D<float> _offset;
    private float _followDistance = 5f;
    private float _smoothness = 5f;

    public Vector3D<float> TargetPosition { get; set; } = Vector3D<float>.Zero;

    public Vector3D<float> Offset
    {
        get => _offset;
        set => _offset = value;
    }

    public float FollowDistance
    {
        get => _followDistance;
        set
        {
            if (MathF.Abs(_followDistance - value) > Epsilon)
            {
                _followDistance = Math.Max(value, 0.1f);
            }
        }
    }

    public float FollowHeight { get; set; } = 2f;

    public float Smoothness
    {
        get => _smoothness;
        set
        {
            if (MathF.Abs(_smoothness - value) > Epsilon)
            {
                _smoothness = Math.Max(value, 0.1f);
            }
        }
    }

    public FollowCamera(string name = "FollowCamera")
    {
        Name = name;
        // Default: follow from behind and above
        _offset = new Vector3D<float>(0, FollowHeight, _followDistance);
    }

    /// <summary>
    /// Sets the target position for the camera to follow.
    /// </summary>
    /// <param name="targetPosition">The position to follow</param>
    public void SetTarget(Vector3D<float> targetPosition)
    {
        TargetPosition = targetPosition;
    }

    /// <summary>
    /// Rotates around the target with a specific offset angle and height.
    /// </summary>
    /// <param name="angleY">Horizontal rotation angle in radians</param>
    /// <param name="height">Height offset from the target</param>
    public void RotateAroundTarget(float angleY, float height)
    {
        var x = FollowDistance * MathF.Sin(angleY);
        var z = FollowDistance * MathF.Cos(angleY);
        _offset = new Vector3D<float>(x, height, z);
    }

    /// <summary>
    /// Adjusts the distance and height of the camera from the target.
    /// </summary>
    /// <param name="distance">Distance behind the target</param>
    /// <param name="height">Height above the target</param>
    public void SetFollowOffset(float distance, float height)
    {
        FollowDistance = distance;
        FollowHeight = height;
        _offset = new Vector3D<float>(_offset.X, height, distance);
    }

    /// <summary>
    /// Updates the camera position to follow the target with smooth interpolation.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        var targetCameraPosition = TargetPosition + _offset;
        var deltaTime = gameTime.GetElapsedSeconds();

        // Smooth interpolation for camera position using exponential damping
        var lerpFactor = 1f - MathF.Exp(-_smoothness * deltaTime);
        lerpFactor = Math.Clamp(lerpFactor, 0f, 1f);
        Position = Vector3D.Lerp(Position, targetCameraPosition, lerpFactor);

        // Always look at the target
        LookAt(TargetPosition, new Vector3D<float>(0, 1, 0));
    }
}
