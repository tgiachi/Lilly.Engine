using System.Numerics;
using Lilly.Engine.Cameras.Base;
using Lilly.Engine.Core.Data.Privimitives;

namespace Lilly.Engine.Cameras;

/// <summary>
/// A first-person camera that moves and rotates in typical FPS style.
/// Movement is relative to the camera's orientation (forward, right, up).
/// </summary>
public class FPSCamera : Base3dCamera
{
    private const float Epsilon = 1e-6f;
    private float _movementSpeed = 5f;
    private float _mouseSensitivity = 0.003f;

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

    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set
        {
            if (MathF.Abs(_mouseSensitivity - value) > Epsilon)
            {
                _mouseSensitivity = Math.Max(value, 0.0001f);
            }
        }
    }

    public float MaxPitchAngle { get; set; } = MathF.PI / 2f - 0.1f;

    public float CurrentPitch { get; private set; }

    public float CurrentYaw { get; private set; }

    public FPSCamera(string name = "FPSCamera")
    {
        Name = name;
        Position = Vector3.Zero;
    }

    /// <summary>
    /// Rotates the camera with pitch (looking up/down) and yaw (looking left/right).
    /// Yaw rotates around the world Y axis, pitch is clamped to prevent flipping.
    /// </summary>
    /// <param name="pitchDelta">Change in pitch in radians (positive = look up)</param>
    /// <param name="yawDelta">Change in yaw in radians (positive = look right)</param>
    public void Look(float pitchDelta, float yawDelta)
    {
        // Update yaw first (rotate around world Y axis)
        if (MathF.Abs(yawDelta) > Epsilon)
        {
            var yawRotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), yawDelta);
            Rotation = yawRotation * Rotation;
            CurrentYaw += yawDelta;
        }

        // Then update pitch (rotate around camera's Right axis)
        if (MathF.Abs(pitchDelta) > Epsilon)
        {
            // Calculate new pitch and clamp
            var newPitch = CurrentPitch + pitchDelta;
            newPitch = Math.Clamp(newPitch, -MaxPitchAngle, MaxPitchAngle);

            // Only apply if within bounds
            var actualPitchDelta = newPitch - CurrentPitch;
            if (MathF.Abs(actualPitchDelta) > Epsilon)
            {
                var pitchRotation = Quaternion.CreateFromAxisAngle(Right, actualPitchDelta);
                Rotation = pitchRotation * Rotation;
                CurrentPitch = newPitch;
            }
        }

        // Update target to be in front of the camera
        Target = Position + Forward;
    }

    /// <summary>
    /// Rotates the camera based on mouse movement delta.
    /// </summary>
    /// <param name="mouseDeltaX">Horizontal mouse movement in pixels</param>
    /// <param name="mouseDeltaY">Vertical mouse movement in pixels</param>
    public void LookWithMouse(float mouseDeltaX, float mouseDeltaY)
    {
        var pitchDelta = -mouseDeltaY * _mouseSensitivity;
        var yawDelta = mouseDeltaX * _mouseSensitivity;

        Look(pitchDelta, yawDelta);
    }

    /// <summary>
    /// Moves the camera in FPS style (forward/back/left/right/up/down).
    /// </summary>
    /// <param name="forward">Forward movement amount (-1 to 1)</param>
    /// <param name="right">Right movement amount (-1 to 1)</param>
    /// <param name="up">Up movement amount (-1 to 1)</param>
    /// <param name="deltaTime">Time elapsed since last frame</param>
    public void MoveInFPSStyle(float forward, float right, float up, float deltaTime)
    {
        var moveVector = Forward * forward + Right * right + new Vector3(0, 1, 0) * up;

        if (moveVector.LengthSquared() > Epsilon)
        {
            moveVector = Vector3.Normalize(moveVector);
            Move(moveVector * _movementSpeed * deltaTime);
        }

        // Update target to stay in front of camera
        Target = Position + Forward;
    }

    /// <summary>
    /// Resets the camera rotation to look forward (no pitch or yaw).
    /// </summary>
    public void ResetRotation()
    {
        CurrentPitch = 0f;
        CurrentYaw = 0f;
        Rotation = Quaternion.Identity;
        Target = Position + Forward;
    }

    /// <summary>
    /// Updates the camera (called each frame).
    /// Override this to add custom input handling for your game.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        // Base update - override in derived class or use Look() and MoveInFPSStyle() methods
        // to handle input from your game's input system
        Target = Position + Forward;
    }
}
