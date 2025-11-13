using ImGuiNET;
using Lilly.Engine.Debuggers.Base;
using Lilly.Engine.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Debuggers;

public class InputDebugger : BaseDebugger
{
    private readonly IInputManagerService _inputManagerService;

    public InputDebugger(IInputManagerService inputManagerService) : base("Input debugger")
    {
        _inputManagerService = inputManagerService;
    }

    public override void DrawImGui()
    {
        if ((_inputManagerService.CurrentMouseState == null) && (_inputManagerService.CurrentKeyboardState == null))
        {
            ImGui.Text("No input states available.");
            return;
        }

        ImGui.LabelText(_inputManagerService.CurrentFocus?.Name ?? "None", "Current Input Focus");

        ImGui.LabelText(_inputManagerService.CurrentContext ?? "None", "Current Input Context");

        ImGui.CollapsingHeader("Mouse", ImGuiTreeNodeFlags.DefaultOpen);
        ImGui.LabelText(_inputManagerService.CurrentMouseState.Position.X.ToString(), "X");
        ImGui.LabelText(_inputManagerService.CurrentMouseState.Position.Y.ToString(), "Y");

        foreach (var pressedButton in _inputManagerService.CurrentMouseState.GetPressedButtons())
        {
            ImGui.BulletText(pressedButton.ToString());
        }

        foreach (var scrollWheel in _inputManagerService.CurrentMouseState.GetScrollWheels())
        {
            ImGui.BulletText($"Scroll: X: {scrollWheel.X}, Y: {scrollWheel.Y}");
        }


        ImGui.CollapsingHeader("Keyboard", ImGuiTreeNodeFlags.DefaultOpen);
        foreach (var pressedKey in _inputManagerService.PressedKeys)
        {
            ImGui.BulletText(pressedKey.ToString());
        }





    }
}
