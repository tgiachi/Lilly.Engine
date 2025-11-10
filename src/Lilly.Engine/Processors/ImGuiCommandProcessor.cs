using ImGuiNET;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Commands;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Lilly.Engine.Processors;

/// <summary>
/// Processes ImGui render commands for debug UI rendering.
/// Batches all ImGui commands and renders them together for optimal performance.
/// </summary>
public class ImGuiCommandProcessor : ICommandProcessor
{
    private readonly RenderContext _renderContext;
    private ImGuiController? _imGuiController;
    private readonly List<ImGuiDataPayload> _batchedPayloads = new(64);
    private bool _isProcessingBatch = false;

    public RenderCommandType CommandType => RenderCommandType.ImGui;

    public ImGuiCommandProcessor(RenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    /// <summary>
    /// Initializes the ImGui controller. Should be called during pipeline initialization.
    /// </summary>
    public void Initialize()
    {
        _imGuiController ??= new(_renderContext.Gl, _renderContext.Window, _renderContext.InputContext);
    }

    /// <summary>
    /// Updates the ImGui controller. Should be called every frame before processing commands.
    /// </summary>
    public void Update(float deltaTime)
    {
        _imGuiController?.Update(deltaTime);
    }

    /// <summary>
    /// Processes a single ImGui command by batching it.
    /// Call FinishBatch() after processing all commands to render.
    /// </summary>
    public void Process(RenderCommand command)
    {
        // Lazy initialization
        _imGuiController ??= new(_renderContext.Gl, _renderContext.Window, _renderContext.InputContext);

        var payload = command.GetPayload<ImGuiDataPayload>();

        if (!_isProcessingBatch)
        {
            _isProcessingBatch = true;
            _batchedPayloads.Clear();
        }

        _batchedPayloads.Add(payload);
    }

    /// <summary>
    /// Renders all batched ImGui commands.
    /// Should be called after all ImGui commands have been processed.
    /// </summary>
    public void FinishBatch()
    {
        if (!_isProcessingBatch || _imGuiController == null)
            return;

        foreach (var payload in _batchedPayloads)
        {
            ImGui.Begin(payload.Name);
            payload.DrawAction();
            ImGui.End();
        }

        _imGuiController.Render();

        _batchedPayloads.Clear();
        _isProcessingBatch = false;
    }
}
