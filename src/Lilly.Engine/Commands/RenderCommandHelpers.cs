using Lilly.Engine.Data.Payloads;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Commands;

/// <summary>
/// Provides helper methods for creating render commands.
/// </summary>
public static class RenderCommandHelpers
{
    /// <summary>
    /// Creates a render command for clearing the screen.
    /// </summary>
    /// <param name="payload">The clear payload containing the clear operation details.</param>
    /// <returns>A render command configured for clear operations.</returns>
    public static RenderCommand CreateClear(ClearPayload payload)
        => new(RenderCommandType.Clear, payload);

    /// <summary>
    /// Creates a render command for drawing text.
    /// </summary>
    /// <param name="payload">The draw text payload containing text rendering details.</param>
    /// <returns>A render command configured for text rendering.</returns>
    public static RenderCommand CreateDrawText(DrawTextPayload payload)
        => new(RenderCommandType.DrawText, payload);

    /// <summary>
    /// Creates a render command for drawing a texture.
    /// </summary>
    /// <param name="payload">The draw texture payload containing texture rendering details.</param>
    /// <returns>A render command configured for texture rendering.</returns>
    public static RenderCommand CreateDrawTexture(DrawTexturePayload payload)
        => new(RenderCommandType.DrawTexture, payload);

    /// <summary>
    /// Creates a render command for ImGui rendering.
    /// </summary>
    /// <param name="payload">The ImGui data payload containing UI rendering details.</param>
    /// <returns>A render command configured for ImGui operations.</returns>
    public static RenderCommand ImGuiRender(ImGuiDataPayload payload)
        => new(RenderCommandType.ImGui, payload);
}
