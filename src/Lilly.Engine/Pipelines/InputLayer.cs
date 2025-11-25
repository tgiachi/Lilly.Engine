using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Input;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;

namespace Lilly.Engine.Pipelines;

public class InputLayer : BaseRenderLayer<IInputReceiver>
{
    private readonly IInputManagerService _inputManagerService;

    public InputLayer(IInputManagerService inputManagerService) : base("Input", RenderPriority.Input)
    {
        _inputManagerService = inputManagerService;
    }

    public override void Update(GameTime gameTime)
    {
        _inputManagerService.Update(gameTime);
        _inputManagerService.DistributeInput(gameTime);
    }


}
