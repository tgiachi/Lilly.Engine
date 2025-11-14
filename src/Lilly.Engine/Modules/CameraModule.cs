using Lilly.Engine.Cameras;
using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Interfaces.Services;

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

    [ScriptFunction("dispatch_mouse", "Dispatches mouse movement to the current camera for rotation.")]
    public void DispatchMouse(float yaw, float pitch, float roll)
    {
        _camera3dService.CurrentCamera?.Rotate(yaw, pitch, roll);
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
