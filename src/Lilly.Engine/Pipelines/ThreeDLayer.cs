using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Entities.Transparent;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;
using Serilog;
using Silk.NET.OpenGL;
using TrippyGL;

namespace Lilly.Engine.Pipelines;

public class ThreeDLayer : BaseRenderLayer<IGameObject3d>
{
    private readonly RenderContext _renderContext;

    public bool IsWireframe { get; set; }
    public float WireframeLineWidth { get; set; } = 1.0f;

    private readonly ICamera3dService _camera3dService;

    public List<IGameObject3d> EntitiesInCullingFrustum { get; } = new();

    public List<IGameObject3d> EntitiesOutsideCullingFrustum { get; } = new();

    private readonly ILogger _logger = Log.ForContext<ThreeDLayer>();

    public ThreeDLayer(RenderContext renderContext, ICamera3dService camera3dService) : base("ThreeD", RenderPriority.ThreeD)
    {
        _renderContext = renderContext;
        _camera3dService = camera3dService;
    }

    public override void Initialize()
    {
        _camera3dService.UpdateViewport(_renderContext.GraphicsDevice.Viewport);
        base.Initialize();
    }

    public override void Render(GameTime gameTime)
    {
        _renderContext.OpenGl.Enable(GLEnum.Multisample);

        // Revert to CullFront (though irrelevant if culling is disabled below) to match original state
        _renderContext.GraphicsDevice.CullFaceMode = CullingMode.CullFront;

        StartRenderTimer();

        if (_camera3dService.ActiveCamera == null)
        {
            EndRenderTimer();

            return;
        }

        EntitiesInCullingFrustum.Clear();
        EntitiesOutsideCullingFrustum.Clear();
        ProcessedEntityCount = 0;
        SkippedEntityCount = 0;

        CheckWireframe();
        _renderContext.GraphicsDevice.DepthState = DepthState.Default;
        _renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;

        // 1. Filter and Categorize (Culling)
        foreach (var entity in Entities)
        {
            if (entity.IsActive && _camera3dService.ActiveCamera.IsInFrustum(entity))
            {
                EntitiesInCullingFrustum.Add(entity);
            }
            else
            {
                EntitiesOutsideCullingFrustum.Add(entity);
                SkippedEntityCount++;
            }
        }

        // 2. Sort (Front-to-Back for Early-Z Optimization)
        var cameraPos = _camera3dService.ActiveCamera.Position;

        EntitiesInCullingFrustum.Sort(
            (a, b) =>
            {
                var distA = Vector3.DistanceSquared(a.Transform.Position, cameraPos);
                var distB = Vector3.DistanceSquared(b.Transform.Position, cameraPos);

                return distA.CompareTo(distB); // Ascending order (Near -> Far)
            }
        );

        // 3. Draw Opaque Pass (Front-to-Back)
        foreach (var entity in EntitiesInCullingFrustum)
        {

            entity.Draw(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);

            ProcessedEntityCount++;
        }

        // 4. Draw Transparent Pass (Back-to-Front)
        for (int i = EntitiesInCullingFrustum.Count - 1; i >= 0; i--)
        {
            var entity = EntitiesInCullingFrustum[i];

            if (entity is ITransparentRenderable3d transparentEntity)
                transparentEntity.DrawTransparent(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);
        }

        RestoreState();
        EndRenderTimer();
    }

    private void CheckWireframe()
    {
        if (IsWireframe)
        {
            _renderContext.OpenGl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
            _renderContext.OpenGl.Disable(GLEnum.CullFace);
            _renderContext.OpenGl.LineWidth(WireframeLineWidth);
        }
        else
        {
            // Disable culling to prevent "missing faces" if vertices are not strictly wound
            _renderContext.GraphicsDevice.FaceCullingEnabled = false;
        }
    }

    private void RestoreState()
    {
        _renderContext.OpenGl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
        _renderContext.GraphicsDevice.DepthState = DepthState.None;
        _renderContext.GraphicsDevice.BlendState = BlendState.Opaque;
    }
}
