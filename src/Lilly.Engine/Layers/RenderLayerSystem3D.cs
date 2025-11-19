using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Helpers;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Lilly.Engine.Layers;

public class RenderLayerSystem3D : BaseRenderLayerSystem<IGameObject3D>
{
    private readonly ICamera3dService _camera3dService;

    private readonly IAudioService _audioService;
    private readonly RenderContext _renderContext;

    private readonly ILogger _logger = Log.ForContext<RenderLayerSystem3D>();

    public HashSet<IGameObject3D> ObjectInFrustum { get; } = [];

    public HashSet<IGameObject3D> ObjectOutOfFrustum { get; } = [];

    public bool IsWireframeMode { get; set; }

    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes
        => new HashSet<RenderCommandType>()
        {
            RenderCommandType.DrawArray,
            RenderCommandType.SetDepthState,
            RenderCommandType.SetCullMode,
            RenderCommandType.SetUniforms
        };

    public RenderLayerSystem3D(
        ICamera3dService camera3dService,
        RenderContext renderContext,
        IAudioService audioService
    ) : base(
        "3d",
        RenderLayer.ThreeDimension
    )
    {
        _camera3dService = camera3dService;
        _renderContext = renderContext;
        _audioService = audioService;
        _camera3dService.UpdateViewport(renderContext.GraphicsDevice.Viewport);
    }

    public override List<RenderCommand> CollectRenderCommands(GameTime gameTime)
    {
        RenderCommands.Clear();
        var camera = _camera3dService.ActiveCamera;

        if (camera == null)
        {
            //_logger.Debug("Dropping render commands for 3D layer because there is no active camera.");

            return [];
        }

        RenderCommands.Add(RenderCommandHelpers.SetWireframeMode(IsWireframeMode));

        ObjectInFrustum.Clear();
        ObjectOutOfFrustum.Clear();

        foreach (var gameObject in GetAllTypedGameObjects())
        {
            if (!gameObject.IsVisible)
            {
                continue;
            }

            gameObject.Draw(camera, gameTime);

            if (IsInFrustum(gameObject, camera))
            {
                RenderCommands.AddRange(gameObject.Render(gameTime));
                ObjectInFrustum.Add(gameObject);
            }
            else
            {
                ObjectOutOfFrustum.Add(gameObject);
            }
        }

        return RenderCommands;
    }

    public override void Add(IGameObject gameObject)
    {
        base.Add(gameObject);

        if (gameObject is IGameObject3D gameObject3D)
        {
            gameObject3D.Initialize();
        }
    }

    public static bool IsInFrustum(IGameObject3D gameObject, ICamera3D camera)
    {
        if (gameObject.IgnoreFrustumCulling)
        {
            return true;
        }

        var position = gameObject.Transform.Position;
        var scale = gameObject.Transform.Scale;

        // Calculate bounding sphere radius for a unit cube (vertices from -0.5 to +0.5)
        // Corner is at (0.5*sx, 0.5*sy, 0.5*sz), so:
        // radius = sqrt((0.5*sx)² + (0.5*sy)² + (0.5*sz)²) = 0.5 * sqrt(sx² + sy² + sz²)
        var scaleLength = MathF.Sqrt(scale.X * scale.X + scale.Y * scale.Y + scale.Z * scale.Z);
        var estimatedRadius = scaleLength * 0.5f;

        return camera.Frustum.Intersects(position, estimatedRadius);
    }

    private void ProcessDrawArrayCommand(DrawArrayPayload payload)
    {
        _renderContext.GraphicsDevice.ShaderProgram = payload.ShaderProgram;
        _renderContext.GraphicsDevice.VertexArray = payload.VertexArray;
        _renderContext.GraphicsDevice.DrawArrays(payload.PrimitiveType, 0, payload.VertexCount);
    }

    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        foreach (var cmd in renderCommands)
        {
            switch (cmd.CommandType)
            {
                case RenderCommandType.DrawArray:
                    var drawArrayPayload = cmd.GetPayload<DrawArrayPayload>();
                    ProcessDrawArrayCommand(drawArrayPayload);

                    break;

                case RenderCommandType.UseShader:
                    var useShaderPayload = cmd.GetPayload<UseShaderPayload>();
                    _renderContext.GraphicsDevice.ShaderProgram = useShaderPayload.ShaderProgram;

                    break;

                case RenderCommandType.SetDepthState:
                    var depthState = cmd.GetPayload<SetDepthStatePayload>();
                    ProcessDepthState(depthState);

                    break;

                case RenderCommandType.SetCullMode:
                    var cullMode = cmd.GetPayload<SetCullModePayload>();
                    ProcessCullMode(cullMode);

                    break;

                case RenderCommandType.SetUniforms:
                    var uniformsPayload = cmd.GetPayload<SetUniformsPayload>();
                    uniformsPayload.Apply();

                    break;
            }
        }
    }

    private void ProcessDepthState(SetDepthStatePayload state)
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

        var depthFunc = state.DepthFunction switch
        {
            Rendering.Core.Types.DepthFunction.Never        => GLEnum.Never,
            Rendering.Core.Types.DepthFunction.Less         => GLEnum.Less,
            Rendering.Core.Types.DepthFunction.Equal        => GLEnum.Equal,
            Rendering.Core.Types.DepthFunction.LessEqual    => GLEnum.Lequal,
            Rendering.Core.Types.DepthFunction.Greater      => GLEnum.Greater,
            Rendering.Core.Types.DepthFunction.NotEqual     => GLEnum.Notequal,
            Rendering.Core.Types.DepthFunction.GreaterEqual => GLEnum.Gequal,
            Rendering.Core.Types.DepthFunction.Always       => GLEnum.Always,
            _                                               => GLEnum.Less
        };

        _renderContext.Gl.DepthFunc(depthFunc);
    }

    private void ProcessCullMode(SetCullModePayload cullMode)
    {
        switch (cullMode.CullMode)
        {
            case CullFaceMode.None:
                _renderContext.Gl.Disable(GLEnum.CullFace);

                break;

            case CullFaceMode.Back:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.Back);

                break;

            case CullFaceMode.Front:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.Front);

                break;

            case CullFaceMode.FrontAndBack:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.FrontAndBack);

                break;

            default:
                _renderContext.Gl.Enable(GLEnum.CullFace);
                _renderContext.Gl.CullFace(GLEnum.Back);

                break;
        }
    }

    public override void Update(GameTime gameTime)
    {
        _camera3dService.Update(gameTime);

        if (_camera3dService.ActiveCamera != null)
        {
            _audioService.Update(_camera3dService.ActiveCamera);
        }

        base.Update(gameTime);
    }
}
