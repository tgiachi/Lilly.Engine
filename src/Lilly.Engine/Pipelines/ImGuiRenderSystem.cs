using ImGuiNET;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Lilly.Engine.Pipelines;

/// <summary>
/// Provides a render system for ImGui-based user interface debugging tools.
/// </summary>
public class ImGuiRenderSystem : BaseRenderLayer<IImGuiDebugger>, IDisposable
{
    private ImGuiController _imGuiController;
    private readonly RenderContext _renderContext;

    public ImGuiRenderSystem(RenderContext renderContext) : base("ImGUI", RenderPriority.Debug)
    {
        _renderContext = renderContext;
    }

    public override void Initialize()
    {
        _imGuiController = new ImGuiController(_renderContext.OpenGl, _renderContext.Window, _renderContext.Input);

        base.Initialize();
    }

    public override void Render(GameTime gameTime)
    {
        _imGuiController.Update((float)gameTime.ElapsedGameTime);

        foreach (var debugger in Entities)
        {
            ImGui.Begin(debugger.Title);
            debugger.Draw();
            ImGui.End();
        }

        ImGui.ShowDemoWindow();

        _imGuiController.Render();

        base.Render(gameTime);
    }

    public void Dispose()
    {
        _imGuiController.Dispose();
        GC.SuppressFinalize(this);
    }
}
