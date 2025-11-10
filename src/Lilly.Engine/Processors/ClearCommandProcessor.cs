using Lilly.Engine.Commands;
using Lilly.Engine.Data.Payloads;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Commands;
using Lilly.Engine.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Engine.Processors;

/// <summary>
/// Processes Clear render commands by clearing the screen with a specified color.
/// </summary>
public class ClearCommandProcessor : ICommandProcessor
{
    private readonly RenderContext _renderContext;

    public RenderCommandType CommandType => RenderCommandType.Clear;

    public ClearCommandProcessor(RenderContext renderContext)
    {
        _renderContext = renderContext;
    }

    public void Process(RenderCommand command)
    {
        var payload = command.GetPayload<ClearPayload>();
        _renderContext.GraphicsDevice.ClearColor = payload.Color.ToVector4();
        _renderContext.GraphicsDevice.Clear(ClearBuffers.Color | ClearBuffers.Depth);
    }
}
