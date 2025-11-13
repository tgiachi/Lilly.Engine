using ImGuiNET;
using Lilly.Engine.Debuggers.Base;
using Lilly.Engine.Interfaces.Services;

namespace Lilly.Engine.Debuggers;

public class CameraDebugger : BaseDebugger
{
    private readonly ICamera3dService _camera3dService;

    public CameraDebugger(ICamera3dService camera3dService) : base("Camera Debugger")
    {
        _camera3dService = camera3dService;
    }

    public override void DrawImGui()
    {
        ImGui.LabelText(_camera3dService.ActiveCamera?.Name ?? "No Active Camera", "Current Active Camera");
    }
}
