using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;
using Serilog;

namespace Lilly.Engine.Layers;

public class RenderLayerSystem3D : BaseRenderLayerSystem<IGameObject3D>
{
    private readonly ICamera3dService _camera3dService;

    public RenderLayerSystem3D(ICamera3dService camera3dService, RenderContext renderContext) : base(
        "3d",
        RenderLayer.ThreeDimension
    )
    {
        _camera3dService = camera3dService;
        _camera3dService.UpdateViewport(renderContext.GraphicsDevice.Viewport);
    }

    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands) { }

    public override void Update(GameTime gameTime)
    {
        _camera3dService.Update(gameTime);
        base.Update(gameTime);
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
        }

        return RenderCommands;
    }

    public static bool IsInFrustum(IGameObject3D gameObject, ICamera3D camera)
    {
        var position = gameObject.Transform.Position;
        var maxScale = MathF.Max(
            MathF.Max(gameObject.Transform.Scale.X, gameObject.Transform.Scale.Y),
            gameObject.Transform.Scale.Z
        );

        // Estimate radius based on max scale (assuming uniform scaling for simplicity)
        var estimatedRadius = maxScale * 2f;

        return camera.Frustum.Intersects(position, estimatedRadius);
    }
}
