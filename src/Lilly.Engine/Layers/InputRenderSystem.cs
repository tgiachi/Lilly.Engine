using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.Input;

namespace Lilly.Engine.Layers;

public class InputRenderSystem : BaseRenderLayerSystem<IInputReceiver>
{
    /// <summary>
    /// This layer processes input commands
    /// </summary>
    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; } =
        new HashSet<RenderCommandType>
        {
            RenderCommandType.None
        };

    private readonly IInputManagerService _inputManagerService;

    private readonly RenderContext _renderContext;

    /// <summary>
    /// Initializes a new instance of the InputRenderSystem class.
    /// </summary>
    /// <param name="inputManagerService">The input manager service.</param>
    public InputRenderSystem(IInputManagerService inputManagerService, RenderContext renderContext) : base(
        "InputLayer",
        RenderLayer.Input
    )
    {
        _inputManagerService = inputManagerService;
        _renderContext = renderContext;
    }

    /// <summary>
    /// Updates the input render system by updating the input manager and distributing input.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public override void Update(GameTime gameTime)
    {
        _inputManagerService.Update(gameTime);

        _inputManagerService.UpdateFocusFromMouse(GetAllTypedGameObjects());

        _inputManagerService.DistributeInput(gameTime);

        _renderContext.InputContext.Mice[0].Cursor.CursorMode =
            _inputManagerService.IsMouseVisible ? CursorMode.Normal : CursorMode.Disabled;

        base.Update(gameTime);
    }
}
