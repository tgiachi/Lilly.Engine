namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload data for ImGui rendering commands.
/// </summary>
public readonly struct ImGuiDataPayload
{
    /// <summary>
    /// Gets the name identifier for this ImGui element.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the action that draws the ImGui interface.
    /// </summary>
    public Action DrawAction { get; init; }

    /// <summary>
    /// Initializes a new instance of the ImGuiDataPayload struct.
    /// </summary>
    /// <param name="name">The name identifier.</param>
    /// <param name="drawAction">The action to execute for drawing.</param>
    public ImGuiDataPayload(string name, Action drawAction)
    {
        Name = name;
        DrawAction = drawAction;
    }
}
