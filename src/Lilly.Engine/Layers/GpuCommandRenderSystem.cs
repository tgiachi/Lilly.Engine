using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Payloads.GpuSubCommands;
using Lilly.Engine.Rendering.Core.Types;
using Silk.NET.OpenGL;
using TrippyGL;

namespace Lilly.Engine.Layers;

public class GpuCommandRenderSystem : BaseRenderLayerSystem<IGameObject>
{
    private readonly RenderContext _renderContext;

    public Color4b ClearColor { get; set; } = Color4b.Black;

    /// <summary>
    /// This layer processes Clear and Window commands.
    /// </summary>
    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; } =
        new HashSet<RenderCommandType>
        {
            RenderCommandType.Clear,
            RenderCommandType.Window,
            RenderCommandType.GpuCommand
        };

    public GpuCommandRenderSystem(RenderContext renderContext) : base(
        "GpuCommandSystem",
        RenderLayer.Background
    )
    {
        _renderContext = renderContext;
    }

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
        // _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        // _renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        // _renderContext.GraphicsDevice.CullFaceMode = CullingMode.CullBack;
        _renderContext.Gl.Enable(GLEnum.Multisample);

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

                case RenderCommandType.GpuCommand:
                    var gpuPayload = cmd.GetPayload<GpuCommandPayload>();
                    ProcessGpuCommand(gpuPayload);

                    break;
            }
        }
        base.ProcessRenderCommands(ref renderCommands);
    }

    private void ProcessGpuCommand(GpuCommandPayload payload)
    {
        switch (payload.CommandType)
        {
            case GpuSubCommandType.SetWireframeMode:
                _renderContext.Gl.PolygonMode(
                    TriangleFace.FrontAndBack,
                    payload.GetPayloadAs<SetWireframeMode>().Enabled ? PolygonMode.Line : PolygonMode.Fill
                );

                break;

            // case GpuSubCommandType.SetDepthState:
            //     var depthState = payload.GetPayloadAs<SetDepthState>();
            //     ProcessDepthState(depthState);
            //
            //     break;
            //
            // case GpuSubCommandType.SetCullMode:
            //     var cullMode = payload.GetPayloadAs<SetCullMode>();
            //     ProcessCullMode(cullMode);

                break;
        }
    }

    private void ProcessDepthState(SetDepthState state)
    {
        if (state.DepthTestEnabled)
        {
            _renderContext.Gl.Enable(GLEnum.DepthTest);
        }
        else
        {
            _renderContext.Gl.Disable(GLEnum.DepthTest);
        }

        _renderContext.Gl.DepthMask(state.DepthWriteEnabled);

        // Map depth function enum to OpenGL enum
        var depthFunc = state.DepthFunction switch
        {
            Rendering.Core.Types.DepthFunction.Never => GLEnum.Never,
            Rendering.Core.Types.DepthFunction.Less => GLEnum.Less,
            Rendering.Core.Types.DepthFunction.Equal => GLEnum.Equal,
            Rendering.Core.Types.DepthFunction.LessEqual => GLEnum.Lequal,
            Rendering.Core.Types.DepthFunction.Greater => GLEnum.Greater,
            Rendering.Core.Types.DepthFunction.NotEqual => GLEnum.Notequal,
            Rendering.Core.Types.DepthFunction.GreaterEqual => GLEnum.Gequal,
            Rendering.Core.Types.DepthFunction.Always => GLEnum.Always,
            _ => GLEnum.Less
        };

        _renderContext.Gl.DepthFunc(depthFunc);
    }

    private void ProcessCullMode(SetCullMode cullMode)
    {
        switch (cullMode.CullMode)
        {
            case Rendering.Core.Types.CullFaceMode.None:
                _renderContext.Gl.Disable(GLEnum.CullFace);
                break;

            case Rendering.Core.Types.CullFaceMode.Back:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.Back);
                break;

            case Rendering.Core.Types.CullFaceMode.Front:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.Front);
                break;

            case Rendering.Core.Types.CullFaceMode.FrontAndBack:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.FrontAndBack);
                break;

            default:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.Back);
                break;
        }
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
                    _renderContext.Renderer.TargetFramesPerSecond = refreshRate;
                }

                break;
        }
    }
}
