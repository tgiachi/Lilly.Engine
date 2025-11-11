using System.Numerics;
using ImGuiNET;
using Lilly.Engine.Core.Attributes.Scripts;

namespace Lilly.Engine.Modules;

/// <summary>
/// Comprehensive ImGui module for Lua scripting.
/// Exposes ImGui functions with Lua-friendly signatures and proper parameter handling.
/// </summary>
[ScriptModule("imGui", "Complete ImGui drawing and interaction library")]
public class ImGuiModule
{

    /// <summary>
    /// Displays text using ImGui.
    /// </summary>
    /// <param name="message">The text to display.</param>
    [ScriptFunction("text")]
    public void Text(string message)
        => ImGui.Text(message);

    /// <summary>
    /// Displays colored text using ImGui.
    /// </summary>
    /// <param name="r">Red component (0-1).</param>
    /// <param name="g">Green component (0-1).</param>
    /// <param name="b">Blue component (0-1).</param>
    /// <param name="a">Alpha component (0-1).</param>
    /// <param name="message">The text to display.</param>
    [ScriptFunction("text_colored")]
    public void TextColored(float r, float g, float b, float a, string message)
        => ImGui.TextColored(new(r, g, b, a), message);

    /// <summary>
    /// Displays disabled text using ImGui.
    /// </summary>
    /// <param name="message">The text to display.</param>
    [ScriptFunction("text_disabled")]
    public void TextDisabled(string message)
        => ImGui.TextDisabled(message);

    /// <summary>
    /// Displays wrapped text using ImGui.
    /// </summary>
    /// <param name="message">The text to display.</param>
    [ScriptFunction("text_wrapped")]
    public void TextWrapped(string message)
        => ImGui.TextWrapped(message);

    /// <summary>
    /// Displays a label with text using ImGui.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="message">The text to display.</param>
    [ScriptFunction("label_text")]
    public void LabelText(string label, string message)
        => ImGui.LabelText(label, message);

    /// <summary>
    /// Displays a bullet point text using ImGui.
    /// </summary>
    /// <param name="message">The text to display.</param>
    [ScriptFunction("bullet_text")]
    public void BulletText(string message)
        => ImGui.BulletText(message);




    [ScriptFunction("button")]
    public bool Button(string label, float width = 0, float height = 0)
        => width > 0 || height > 0
               ? ImGui.Button(label, new(width, height))
               : ImGui.Button(label);

    [ScriptFunction("small_button")]
    public bool SmallButton(string label)
        => ImGui.SmallButton(label);

    [ScriptFunction("invisible_button")]
    public bool InvisibleButton(string id, float width, float height)
        => ImGui.InvisibleButton(id, new(width, height));

    [ScriptFunction("arrow_button")]
    public bool ArrowButton(string id, int direction)
        => ImGui.ArrowButton(id, (ImGuiDir)direction);

    [ScriptFunction("checkbox")]
    public bool Checkbox(string label, bool value)
    {
        var v = value;
        ImGui.Checkbox(label, ref v);

        return v;
    }

    [ScriptFunction("radio_button")]
    public bool RadioButton(string label, int active, int buttonValue)
    {
        var isActive = active == buttonValue;

        if (ImGui.RadioButton(label, isActive))
        {
            return true;
        }

        return false;
    }




    [ScriptFunction("input_text")]
    public string InputText(string label, string text, uint maxLength = 256)
    {
        var value = text;
        ImGui.InputText(label, ref value, maxLength);

        return value;
    }

    [ScriptFunction("input_text_multiline")]
    public string InputTextMultiline(string label, string text, float width, float height, uint maxLength = 1024)
    {
        var value = text;
        ImGui.InputTextMultiline(label, ref value, maxLength, new(width, height));

        return value;
    }

    [ScriptFunction("input_float")]
    public float InputFloat(string label, float value, float step = 0.0f, float stepFast = 0.0f)
    {
        var v = value;
        ImGui.InputFloat(label, ref v, step, stepFast);

        return v;
    }

    [ScriptFunction("input_float2")]
    public string InputFloat2(string label, float x, float y)
    {
        Vector2 v = new(x, y);
        ImGui.InputFloat2(label, ref v);

        return $"{v.X},{v.Y}";
    }

    [ScriptFunction("input_float3")]
    public string InputFloat3(string label, float x, float y, float z)
    {
        Vector3 v = new(x, y, z);
        ImGui.InputFloat3(label, ref v);

        return $"{v.X},{v.Y},{v.Z}";
    }

    [ScriptFunction("input_int")]
    public int InputInt(string label, int value, int step = 1, int stepFast = 100)
    {
        var v = value;
        ImGui.InputInt(label, ref v, step, stepFast);

        return v;
    }

    [ScriptFunction("input_int2")]
    public string InputInt2(string label, int x, int y)
    {
        int[] values = { x, y };
        ImGui.InputInt2(label, ref values[0]);

        return $"{values[0]},{values[1]}";
    }




    [ScriptFunction("slider_float")]
    public float SliderFloat(string label, float value, float min, float max)
    {
        var v = value;
        ImGui.SliderFloat(label, ref v, min, max);

        return v;
    }

    [ScriptFunction("slider_float2")]
    public string SliderFloat2(string label, float x, float y, float min, float max)
    {
        Vector2 v = new(x, y);
        ImGui.SliderFloat2(label, ref v, min, max);

        return $"{v.X},{v.Y}";
    }

    [ScriptFunction("slider_float3")]
    public string SliderFloat3(string label, float x, float y, float z, float min, float max)
    {
        Vector3 v = new(x, y, z);
        ImGui.SliderFloat3(label, ref v, min, max);

        return $"{v.X},{v.Y},{v.Z}";
    }

    [ScriptFunction("slider_int")]
    public int SliderInt(string label, int value, int min, int max)
    {
        var v = value;
        ImGui.SliderInt(label, ref v, min, max);

        return v;
    }

    [ScriptFunction("slider_int2")]
    public string SliderInt2(string label, int x, int y, int min, int max)
    {
        int[] values = { x, y };
        ImGui.SliderInt2(label, ref values[0], min, max);

        return $"{values[0]},{values[1]}";
    }

    [ScriptFunction("vslider_float")]
    public float VSliderFloat(string label, float width, float height, float value, float min, float max)
    {
        var v = value;
        ImGui.VSliderFloat(label, new(width, height), ref v, min, max);

        return v;
    }

    [ScriptFunction("vslider_int")]
    public int VSliderInt(string label, float width, float height, int value, int min, int max)
    {
        var v = value;
        ImGui.VSliderInt(label, new(width, height), ref v, min, max);

        return v;
    }




    [ScriptFunction("color_picker3")]
    public string ColorPicker3(string label, float r, float g, float b)
    {
        Vector3 color = new(r, g, b);
        ImGui.ColorPicker3(label, ref color);

        return $"{color.X},{color.Y},{color.Z}";
    }

    [ScriptFunction("color_picker4")]
    public string ColorPicker4(string label, float r, float g, float b, float a)
    {
        Vector4 color = new(r, g, b, a);
        ImGui.ColorPicker4(label, ref color);

        return $"{color.X},{color.Y},{color.Z},{color.W}";
    }




    [ScriptFunction("separator")]
    public void Separator()
        => ImGui.Separator();

    [ScriptFunction("spacing")]
    public void Spacing()
        => ImGui.Spacing();

    [ScriptFunction("new_line")]
    public void NewLine()
        => ImGui.NewLine();

    [ScriptFunction("same_line")]
    public void SameLine(float offsetFromStart = 0, float spacing = -1)
        => ImGui.SameLine(offsetFromStart, spacing);

    [ScriptFunction("indent")]
    public void Indent(float width = 0)
        => ImGui.Indent(width);

    [ScriptFunction("unindent")]
    public void Unindent(float width = 0)
        => ImGui.Unindent(width);

    [ScriptFunction("dummy")]
    public void Dummy(float width, float height)
        => ImGui.Dummy(new(width, height));




    [ScriptFunction("begin_group")]
    public void BeginGroup()
        => ImGui.BeginGroup();

    [ScriptFunction("end_group")]
    public void EndGroup()
        => ImGui.EndGroup();

    [ScriptFunction("collapsing_header")]
    public bool CollapsingHeader(string label, int flags = 0)
        => ImGui.CollapsingHeader(label, (ImGuiTreeNodeFlags)flags);

    [ScriptFunction("tree_node")]
    public bool TreeNode(string label)
        => ImGui.TreeNode(label);

    [ScriptFunction("tree_node_ex")]
    public bool TreeNodeEx(string label, int flags = 0)
        => ImGui.TreeNodeEx(label, (ImGuiTreeNodeFlags)flags);

    [ScriptFunction("tree_pop")]
    public void TreePop()
        => ImGui.TreePop();

    [ScriptFunction("set_next_item_open")]
    public void SetNextItemOpen(bool open, int cond = 0)
        => ImGui.SetNextItemOpen(open, (ImGuiCond)cond);




    [ScriptFunction("is_item_hovered")]
    public bool IsItemHovered(int flags = 0)
        => ImGui.IsItemHovered((ImGuiHoveredFlags)flags);

    [ScriptFunction("set_item_tooltip")]
    public void SetItemTooltip(string tooltip)
        => ImGui.SetItemTooltip(tooltip);

    [ScriptFunction("begin_tooltip")]
    public void BeginTooltip()
        => ImGui.BeginTooltip();

    [ScriptFunction("end_tooltip")]
    public void EndTooltip()
        => ImGui.EndTooltip();




    [ScriptFunction("open_popup")]
    public void OpenPopup(string id, int flags = 0)
        => ImGui.OpenPopup(id, (ImGuiPopupFlags)flags);

    [ScriptFunction("begin_popup")]
    public bool BeginPopup(string id, int flags = 0)
        => ImGui.BeginPopup(id, (ImGuiWindowFlags)flags);

    [ScriptFunction("begin_popup_modal")]
    public bool BeginPopupModal(string title, int flags = 0)
        => ImGui.BeginPopupModal(title, (ImGuiWindowFlags)flags);

    [ScriptFunction("end_popup")]
    public void EndPopup()
        => ImGui.EndPopup();

    [ScriptFunction("close_current_popup")]
    public void CloseCurrentPopup()
        => ImGui.CloseCurrentPopup();

    [ScriptFunction("is_popup_open")]
    public bool IsPopupOpen(string id, int flags = 0)
        => ImGui.IsPopupOpen(id, (ImGuiPopupFlags)flags);




    [ScriptFunction("get_draw_data")]
    public string GetDrawData()
        => "DrawData retrieved";

    [ScriptFunction("show_demo_window")]
    public void ShowDemoWindow()
    {
        var show = true;
        ImGui.ShowDemoWindow(ref show);
    }

    [ScriptFunction("show_metrics_window")]
    public void ShowMetricsWindow()
    {
        var show = true;
        ImGui.ShowMetricsWindow(ref show);
    }




    [ScriptFunction("push_style_color")]
    public void PushStyleColor(int idx, float r, float g, float b, float a)
        => ImGui.PushStyleColor((ImGuiCol)idx, new Vector4(r, g, b, a));

    [ScriptFunction("pop_style_color")]
    public void PopStyleColor(int count = 1)
        => ImGui.PopStyleColor(count);

    [ScriptFunction("push_style_var_float")]
    public void PushStyleVarFloat(int idx, float val)
        => ImGui.PushStyleVar((ImGuiStyleVar)idx, val);

    [ScriptFunction("push_style_var_vec2")]
    public void PushStyleVarVec2(int idx, float x, float y)
        => ImGui.PushStyleVar((ImGuiStyleVar)idx, new Vector2(x, y));

    [ScriptFunction("pop_style_var")]
    public void PopStyleVar(int count = 1)
        => ImGui.PopStyleVar(count);




    [ScriptFunction("get_io")]
    public string GetIO()
    {
        var io = ImGui.GetIO();

        return $"FPS:{io.Framerate:F1} Mouse:({io.MousePos.X:F0},{io.MousePos.Y:F0}) Delta:{io.DeltaTime:F3}";
    }

    [ScriptFunction("is_mouse_down")]
    public bool IsMouseDown(int button)
        => ImGui.IsMouseDown((ImGuiMouseButton)button);

    [ScriptFunction("is_mouse_clicked")]
    public bool IsMouseClicked(int button)
        => ImGui.IsMouseClicked((ImGuiMouseButton)button);

    [ScriptFunction("get_mouse_pos")]
    public string GetMousePos()
    {
        var pos = ImGui.GetMousePos();

        return $"{pos.X},{pos.Y}";
    }

    [ScriptFunction("is_key_down")]
    public bool IsKeyDown(int key)
        => ImGui.IsKeyDown((ImGuiKey)key);

    [ScriptFunction("is_key_pressed")]
    public bool IsKeyPressed(int key)
        => ImGui.IsKeyPressed((ImGuiKey)key);




    [ScriptFunction("calc_text_size")]
    public string CalcTextSize(string text)
    {
        var size = ImGui.CalcTextSize(text);

        return $"{size.X},{size.Y}";
    }

    [ScriptFunction("get_cursor_pos")]
    public string GetCursorPos()
    {
        var pos = ImGui.GetCursorPos();

        return $"{pos.X},{pos.Y}";
    }

    [ScriptFunction("set_cursor_pos")]
    public void SetCursorPos(float x, float y)
        => ImGui.SetCursorPos(new(x, y));

    [ScriptFunction("get_frame_height")]
    public float GetFrameHeight()
        => ImGui.GetFrameHeight();

    [ScriptFunction("get_frame_height_with_spacing")]
    public float GetFrameHeightWithSpacing()
        => ImGui.GetFrameHeightWithSpacing();

    [ScriptFunction("get_window_pos")]
    public string GetWindowPos()
    {
        var pos = ImGui.GetWindowPos();

        return $"{pos.X},{pos.Y}";
    }

    [ScriptFunction("get_window_size")]
    public string GetWindowSize()
    {
        var size = ImGui.GetWindowSize();

        return $"{size.X},{size.Y}";
    }


}
