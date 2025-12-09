using ImGuiNET;
using Lilly.Engine.GameObjects.Base;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Debuggers;

public class CameraDebugger : BaseImGuiDebuggerGameObject
{
    private readonly ICamera3dService _cameraService;

    public CameraDebugger(ICamera3dService cameraService) : base("Camera Debugger")
        => _cameraService = cameraService;

    protected override void DrawDebug()
    {
        var cameras = _cameraService.Cameras;
        var activeCamera = _cameraService.ActiveCamera;

        ImGui.Text($"Available Cameras: {cameras.Count}");
        ImGui.Separator();

        if (cameras.Count == 0)
        {
            ImGui.Text("No cameras registered.");

            return;
        }

        if (ImGui.BeginCombo("Active Camera", activeCamera?.Name ?? "None"))
        {
            foreach (var camera in cameras)
            {
                var isSelected = camera == activeCamera;

                if (ImGui.Selectable(camera.Name, isSelected))
                {
                    _cameraService.CurrentCamera = camera;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.Separator();

        if (activeCamera == null)
        {
            ImGui.Text("No active camera selected.");

            return;
        }

        ImGui.Text($"Camera: {activeCamera.Name}");
        ImGui.Text($"Enabled: {activeCamera.Enabled}");
        ImGui.Text(
            $"Position: X: {activeCamera.Position.X:F2}, Y: {activeCamera.Position.Y:F2}, Z: {activeCamera.Position.Z:F2}"
        );
        ImGui.Text(
            $"Rotation: Pitch: {activeCamera.Rotation.X:F2}, Yaw: {activeCamera.Rotation.Y:F2}, Roll: {activeCamera.Rotation.Z:F2}"
        );
        ImGui.Text($"FOV: {activeCamera.FieldOfView:F2}");
        ImGui.Text($"Near Clip: {activeCamera.NearPlane:F2}");
        ImGui.Text($"Far Clip: {activeCamera.FarPlane:F2}");
    }
}
