using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using Serilog;
using TrippyGL;

namespace Lilly.Engine.Layers;

public class RenderLayerSystem3D : BaseRenderLayerSystem<IGameObject3D>
{
    private readonly ICamera3dService _camera3dService;

    private readonly RenderContext _renderContext;

    private readonly ILogger _logger = Log.ForContext<RenderLayerSystem3D>();


    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes
        => new HashSet<RenderCommandType>()
        {
            RenderCommandType.DrawArray
        };

    public RenderLayerSystem3D(ICamera3dService camera3dService, RenderContext renderContext) : base(
        "3d",
        RenderLayer.ThreeDimension
    )
    {
        _camera3dService = camera3dService;
        _renderContext = renderContext;
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

        foreach (var gameObject in GetAllTypedGameObjects())
        {
            if (!gameObject.IsVisible)
            {
                continue;
            }

            if (IsInFrustum(gameObject, camera))
            {
                gameObject.Draw(camera, gameTime);
                RenderCommands.AddRange(gameObject.Render(gameTime));
            }
            else
            {
                _logger.Debug(
                    "Culled game object {GameObjectName} from rendering because it is outside the camera frustum.",
                    gameObject.Name
                );
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

        _renderContext.GraphicsDevice.DrawArrays(PrimitiveType.TriangleStrip, 0, payload.VertexCount);
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
            }
        }
    }

    public override void Update(GameTime gameTime)
    {
        _camera3dService.Update(gameTime);
        base.Update(gameTime);
    }
}
