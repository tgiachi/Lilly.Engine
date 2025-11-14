using System.Numerics;
using ImGuiNET;
using Lilly.Engine.Debuggers.Base;
using Lilly.Engine.Interfaces.Services;
using Silk.NET.Maths;

namespace Lilly.Engine.Debuggers;

public class CameraDebugger : BaseDebugger
{
    private readonly ICamera3dService _camera3dService;

    public CameraDebugger(ICamera3dService camera3dService) : base("Camera Debugger")
        => _camera3dService = camera3dService;

    public override void DrawImGui()
    {
        ImGui.LabelText(_camera3dService.ActiveCamera?.Name ?? "No Active Camera", "Current Active Camera");

        // Available Cameras
        if (ImGui.CollapsingHeader("Available Cameras", ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var camera in _camera3dService.Cameras)
            {
                ImGui.BulletText(camera.Name);
            }
        }

        if (_camera3dService.ActiveCamera != null)
        {
            if (ImGui.CollapsingHeader("Camera Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var camera = _camera3dService.ActiveCamera;

                ImGui.LabelText(camera.Position.ToString(), "Position");
                ImGui.LabelText(camera.Rotation.ToString(), "Rotation");
                ImGui.LabelText(camera.FieldOfView.ToString("F2"), "Field of View");
                ImGui.LabelText(camera.AspectRatio.ToString("F2"), "Aspect Ratio");

                var position = camera.Position.ToSystem();
                ImGui.Separator();
                ImGui.Text("Position");

                if (ImGui.InputFloat3("##Position", ref position))
                {
                    camera.Position = new(position.X, position.Y, position.Z);
                }

                ImGui.Separator();
                ImGui.Text("Rotation");

                var rotation = camera.Rotation.ToSystem().AsVector4();

                if (ImGui.InputFloat4("##Rotation", ref rotation))
                {
                    camera.Rotation = new(rotation.X, rotation.Y, rotation.Z, rotation.W);
                }
            }
        }
    }
}
