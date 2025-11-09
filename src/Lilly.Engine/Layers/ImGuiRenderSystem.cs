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

/// <summary>
/// Provides a render system for ImGui-based user interface debugging tools.
/// </summary>
public class ImGuiRenderSystem : BaseRenderLayerSystem<IImGuiDebugger>, IDisposable
{
    private ImGuiController _imGuiController;

    private readonly RenderContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImGuiRenderSystem" /> class.
    /// </summary>
    /// <param name="context">The render context containing window and graphics device information.</param>
    public ImGuiRenderSystem(RenderContext context) : base("ImGui", RenderLayer.UI)
        => _context = context;

    /// <summary>
    /// Disposes the ImGui controller and releases resources.
    /// </summary>
    public void Dispose()
    {
        _imGuiController.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Initializes the ImGui controller with the graphics context.
    /// </summary>
    public override void Initialize()
    {
        _imGuiController = new(_context.Gl, _context.Window, _context.InputContext);
        base.Initialize();
    }

    /// <summary>
    /// Processes render commands for ImGui debug windows and renders them.
    /// </summary>
    /// <param name="renderCommands">The list of render commands to process.</param>
    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        foreach (var command in renderCommands.ToArray())
        {
            var payload = command.GetPayload<ImGuiDataPayload>()!;

            ImGui.Begin(payload.Name);
            payload.DrawAction();
            ImGui.End();
        }
        _imGuiController.Render();
    }

    /// <summary>
    /// Updates the ImGui controller with the current frame timing.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    public override void Update(GameTime gameTime)
    {
        _imGuiController.Update((float)_context.GameTime.ElapsedGameTime);
    }
}
