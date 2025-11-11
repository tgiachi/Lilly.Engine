using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Engine.Layers;

public class GpuCommandRenderSystem : BaseRenderLayerSystem<IGameObject>
{
    private readonly RenderContext _renderContext;

    public Color4b ClearColor { get; set; } = Color4b.BlanchedAlmond;

    /// <summary>
    /// This layer processes Clear and Window commands.
    /// </summary>
    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; } =
        new HashSet<RenderCommandType>
        {
            RenderCommandType.Clear,
            RenderCommandType.Window,
            RenderCommandType.Scissor
        };

    public GpuCommandRenderSystem(RenderContext renderContext) : base("GpuCommandSystem", RenderLayer.Background)
        => _renderContext = renderContext;

    /// <summary>
    /// Collects render commands for clearing the screen and handling window operations.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <returns>A list of render commands.</returns>
    public override List<RenderCommand> CollectRenderCommands(GameTime gameTime)
    {
        RenderCommands.Clear();
        RenderCommands.Add(RenderCommandHelpers.CreateClear(new(ClearColor)));

        return RenderCommands;
    }

    /// <summary>
    /// Processes the render commands for clearing the screen and handling window operations.
    /// </summary>
    /// <param name="renderCommands">The list of render commands to process.</param>
    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        _renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        foreach (var cmd in renderCommands)
        {
            switch (cmd.CommandType)
            {
                case RenderCommandType.Clear:
                    var clearPayload = cmd.GetPayload<ClearPayload>();
                    _renderContext.GraphicsDevice.ClearColor = clearPayload.Color.ToVector4();
                    _renderContext.GraphicsDevice.Clear(ClearBuffers.Color | ClearBuffers.Depth);

                    break;

                case RenderCommandType.Window:
                    var windowPayload = cmd.GetPayload<WindowPayload>();
                    ProcessWindowCommand(windowPayload);

                    break;
                case RenderCommandType.Scissor:
                    var scissorPayload = cmd.GetPayload<ScissorPayload>();
                    ProcessScissorCommand(scissorPayload);

                    break;
            }
        }
        base.ProcessRenderCommands(ref renderCommands);
    }

    private void ProcessWindowCommand(WindowPayload payload)
    {
        switch (payload.SubCommandType)
        {
            case WindowSubCommandType.SetTitle:
                _renderContext.Window.Title = payload.Data as string;

                break;
            case WindowSubCommandType.SetFullscreen:
                if (payload.Data is bool isFullscreen) { }

                break;
            case WindowSubCommandType.SetVSync:
                if (payload.Data is bool isVSync)
                {
                    _renderContext.Window.VSync = isVSync;
                }

                break;
            case WindowSubCommandType.SetRefreshRate:
                if (payload.Data is int refreshRate)
                {
                    _renderContext.Window.FramesPerSecond = refreshRate;
                }

                break;
        }
    }

    private void ProcessScissorCommand(ScissorPayload payload)
    {
        if (payload.IsEnabled)
        {
            _renderContext.GraphicsDevice.ScissorRectangle = new Viewport(
                payload.X,
                payload.Y,
                (uint)payload.Width,
                (uint)payload.Height
            );

            _renderContext.GraphicsDevice.ScissorTestEnabled = true;

            return;
        }

        _renderContext.GraphicsDevice.ScissorTestEnabled = false;
    }
}
