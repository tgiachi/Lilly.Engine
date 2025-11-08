using Lilly.Engine.Data.Payloads;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Commands;

public static class RenderCommandHelpers
{
    public static RenderCommand CreateClear(ClearPayload payload)
    {
        return new RenderCommand(RenderCommandType.Clear, payload);
    }

    public static RenderCommand CreateDrawText(DrawTextPayload payload)
    {
        return new RenderCommand(RenderCommandType.DrawText, payload);
    }

    public static RenderCommand CreateDrawTexture(DrawTexturePayload payload)
    {
        return new RenderCommand(RenderCommandType.DrawTexture, payload);
    }

    public static RenderCommand ImGuiRender(ImGuiDataPayload payload)
    {
        return new RenderCommand(RenderCommandType.ImGui, payload);
    }
}
