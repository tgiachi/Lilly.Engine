using Lilly.Engine.Cameras.Base;
using Lilly.Engine.Core.Data.Privimitives;

namespace Lilly.Engine.Cameras;

/// <summary>
/// A free-moving camera that can be positioned and rotated independently.
/// Useful for debug cameras, editor cameras, or cutscene cameras.
/// </summary>
public class FreeCamera : Base3dCamera
{
    private const float Epsilon = 1e-6f;
    private float _movementSpeed = 10f;
    private float _rotationSpeed = 1f;

    public float MovementSpeed
    {
        get => _movementSpeed;
        set
        {
            if (MathF.Abs(_movementSpeed - value) > Epsilon)
            {
                _movementSpeed = Math.Max(value, 0.1f);
            }
        }
    }

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set
        {
            if (MathF.Abs(_rotationSpeed - value) > Epsilon)
            {
                _rotationSpeed = Math.Max(value, 0.01f);
            }
        }
    }

    public FreeCamera(string name = "FreeCamera")
    {
        Name = name;
    }

    /// <summary>
    /// Rotates the camera using pitch (X), yaw (Y), and roll (Z) angles.
    /// </summary>
    /// <param name="pitch">Pitch rotation in radians (around X axis)</param>
    /// <param name="yaw">Yaw rotation in radians (around Y axis)</param>
    /// <param name="roll">Roll rotation in radians (around Z axis)</param>
    public void RotateCamera(float pitch, float yaw, float roll)
    {
        Rotate(pitch, yaw, roll);
    }

    /// <summary>
    /// Updates the camera based on game time (for animation or smooth movement if needed).
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        // Base update - could be extended for smooth camera animations
        // Currently, movement is handled through explicit method calls
    }
}
