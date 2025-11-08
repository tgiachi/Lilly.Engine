using ImGuiNET;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Payloads;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Lilly.Engine.Layers;

public class ImGuiRenderSystem : BaseRenderLayerSystem<IImGuiDebugger>, IDisposable
{
    private ImGuiController _imGuiController;

    private readonly RenderContext _context;

    public ImGuiRenderSystem(RenderContext context) : base("ImGui", RenderLayer.UI)
    {
        _context = context;
    }

    public override void Initialize()
    {
        _imGuiController = new ImGuiController(_context.Gl, _context.Window, _context.InputContext);
        base.Initialize();
    }

    public override void Update(GameTime gameTime)
    {
        _imGuiController.Update((float)_context.GameTime.ElapsedGameTime);
    }


    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        foreach (var command in renderCommands)
        {
            var payload = command.GetPayload<ImGuiDataPayload>()!;

            ImGui.Begin(payload.Name);
            payload.DrawAction();
            ImGui.End();
        }
        _imGuiController.Render();
    }


    public void Dispose()
    {
        _imGuiController.Dispose();
        GC.SuppressFinalize(this);
    }
}
