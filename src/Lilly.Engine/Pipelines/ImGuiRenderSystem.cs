using ImGuiNET;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Debuggers;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;
using Silk.NET.OpenGL.Extensions.ImGui;
using TrippyGL;

namespace Lilly.Engine.Pipelines;

/// <summary>
/// Provides a render system for ImGui-based user interface debugging tools.
/// </summary>
public class ImGuiRenderSystem : BaseRenderLayer<IImGuiDebugger>, IDisposable
{
    private ImGuiController _imGuiController;
    private readonly RenderContext _renderContext;

    /// <summary>
    /// Initializes a new instance of the ImGuiRenderSystem class.
    /// </summary>
    /// <param name="renderContext">The render context.</param>
    public ImGuiRenderSystem(RenderContext renderContext) : base("ImGUI", RenderPriority.Debug)
    {
        _renderContext = renderContext;
    }

    /// <summary>
    /// Initializes the ImGui controller.
    /// </summary>
    public override void Initialize()
    {
        _imGuiController = new ImGuiController(_renderContext.OpenGl, _renderContext.Window, _renderContext.Input);

        base.Initialize();
    }

    /// <summary>
    /// Renders the ImGui debug windows.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public override void Render(GameTime gameTime)
    {
        StartUpdateTimer();
        _imGuiController.Update((float)gameTime.ElapsedGameTime);

        ProcessedEntityCount = 0;
        foreach (var debugger in Entities)
        {
            ProcessedEntityCount ++;
            ImGui.Begin(debugger.Title);
            debugger.Draw();
            ImGui.End();
        }

        _renderContext.GraphicsDevice.DepthTestingEnabled = false;
        _renderContext.GraphicsDevice.DepthState = DepthState.None;
        _renderContext.GraphicsDevice.Clear(ClearBuffers.Depth);

        _imGuiController.Render();


        _renderContext.GraphicsDevice.DepthTestingEnabled = true;
        _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        base.Render(gameTime);

        EndUpdateTimer();
    }

    /// <summary>
    /// Disposes the ImGui controller.
    /// </summary>
    public void Dispose()
    {
        _imGuiController.Dispose();
        GC.SuppressFinalize(this);
    }
}
