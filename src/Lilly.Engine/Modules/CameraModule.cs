using System.Numerics;
using Lilly.Engine.Cameras;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Modules;

[ScriptModule("camera", "Provides functionality related to camera management.")]
public class CameraModule
{
    private readonly ICamera3dService _camera3dService;

    public CameraModule(ICamera3dService camera3dService)
        => _camera3dService = camera3dService;

    [ScriptFunction("dispatch_keyboard", "Dispatches keyboard input to the current camera for movement.")]
    public void DispatchKeyboard(float forward, float right, float up)
    {
        _camera3dService.CurrentCamera?.Move(new(forward, right, up));
    }

    [ScriptFunction(
        "dispatch_keyboard_fps",
        "Dispatches keyboard input for FPS camera movement relative to camera orientation."
    )]
    public void DispatchKeyboardFps(float forward, float right, float up)
    {
        if (_camera3dService.CurrentCamera is FPSCamera fpsCamera)
        {
            // Forwards movement now follows the camera's true forward vector, including vertical component.
            var actualForward = fpsCamera.Forward * forward;

            // Strafing (right/left) movement is still projected onto the XZ plane for horizontal movement.
            var rightFlat = new Vector3(fpsCamera.Right.X, 0, fpsCamera.Right.Z);

            if (rightFlat.Length() > 0.0001f)
            {
                rightFlat = Vector3.Normalize(rightFlat);
            }
            else
            {
                rightFlat = Vector3.Zero;
            }
            var strafeMove = rightFlat * -right; // Inverted 'right' movement as previously corrected

            // Vertical movement is separate.
            var verticalMove = new Vector3(0, up, 0);

            // Combine and apply all movements
            fpsCamera.Move((actualForward + strafeMove + verticalMove) * 0.5f);

            // CRITICAL: Update target to stay in front of camera after moving
            fpsCamera.Target = fpsCamera.Position + fpsCamera.Forward;
        }
    }

    [ScriptFunction("dispatch_mouse", "Dispatches mouse movement to the current camera for rotation.")]
    public void DispatchMouse(float yaw, float pitch, float roll)
    {
        _camera3dService.CurrentCamera?.Rotate(pitch, yaw, roll);
    }

    [ScriptFunction("dispatch_mouse_fps", "Dispatches mouse delta directly for FPS camera using Look method.")]
    public void DispatchMouseFps(float pitchDelta, float yawDelta)
    {
        // pitchDelta: positive = look up, negative = look down
        // yawDelta: positive = look right, negative = look left
        if (_camera3dService.CurrentCamera is FPSCamera fpsCamera)
        {
            fpsCamera.Look(-pitchDelta, yawDelta);
        }
    }

    [ScriptFunction("register_fps", "Registers a first-person camera with the given name.")]
    public void RegisterFpsCamera(string name)
    {
        var camera = new FPSCamera(name);
        _camera3dService.RegisterCamera(camera);
    }

    [ScriptFunction("set_active", "Sets the active camera by its name.")]
    public void SetActiveCamera(string name)
    {
        var camera = _camera3dService.Cameras.FirstOrDefault(c => c.Name == name);

        if (camera != null)
        {
            _camera3dService.CurrentCamera = camera;
        }
    }
}
