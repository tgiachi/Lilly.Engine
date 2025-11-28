using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
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

        foreach (var entity in Entities)
        {
            if (_camera3dService.ActiveCamera.IsInFrustum(entity) && entity.IsActive)
            {
                entity.Draw(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);
                EntitiesInCullingFrustum.Add(entity);
                ProcessedEntityCount++;
            }
            else
            {
                EntitiesOutsideCullingFrustum.Add(entity);
                SkippedEntityCount++;
            }
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
            _renderContext.GraphicsDevice.CullFaceMode = CullingMode.CullBack;
        }
    }

    private void RestoreState()
    {
        _renderContext.OpenGl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
        _renderContext.GraphicsDevice.DepthState = DepthState.None;
    }
}
