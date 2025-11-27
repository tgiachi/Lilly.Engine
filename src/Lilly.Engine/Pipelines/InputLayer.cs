using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Input;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;
using Silk.NET.Input;

namespace Lilly.Engine.Pipelines;

public class InputLayer : BaseRenderLayer<IInputReceiver>
{
    private readonly IInputManagerService _inputManagerService;

    private readonly RenderContext _renderContext;

    public InputLayer(IInputManagerService inputManagerService, RenderContext renderContext) : base(
        "Input",
        RenderPriority.Input
    )
    {
        _inputManagerService = inputManagerService;
        _renderContext = renderContext;
    }

    public override void Update(GameTime gameTime)
    {
        StartUpdateTimer();
        _renderContext.Input.Mice[0].Cursor.CursorMode = _inputManagerService.IsMouseVisible
                                                             ? CursorMode.Normal
                                                             : CursorMode.Disabled;

        _inputManagerService.Update(gameTime);
        _inputManagerService.DistributeInput(gameTime);

        EndUpdateTimer();
    }
}
