using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Pipelines.Renderers;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Entities.Transparent;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;
using Silk.NET.OpenGL;
using TrippyGL;
using DirectionalLight = Lilly.Rendering.Core.Lights.DirectionalLight;
using PointLight = Lilly.Rendering.Core.Lights.PointLight;
using SpotLight = Lilly.Rendering.Core.Lights.SpotLight;

namespace Lilly.Engine.Pipelines;

public class ThreeDLayer : BaseRenderLayer<IGameObject3d>, IDisposable
{
    private readonly RenderContext _renderContext;
    private readonly IAssetManager _assetManager;
    private readonly ILightManager _lightManager;
    private readonly ICamera3dService _camera3dService;

    private readonly ShadowRenderer _shadowRenderer;
    private readonly DebugRenderer _debugRenderer;

    public bool IsWireframe { get; set; }
    public float WireframeLineWidth { get; set; } = 1.0f;

    // Delegate Debug properties to DebugRenderer
    public bool DebugDrawBoundingBoxes
    {
        get => _debugRenderer.DrawBoundingBoxes;
        set => _debugRenderer.DrawBoundingBoxes = value;
    }

    public bool DebugDrawPhysicsShapes
    {
        get => _debugRenderer.DrawPhysicsShapes;
        set => _debugRenderer.DrawPhysicsShapes = value;
    }

    public Color4b DebugBoundsColor
    {
        get => _debugRenderer.BoundsColor;
        set => _debugRenderer.BoundsColor = value;
    }

    public Color4b DebugPhysicsColor
    {
        get => _debugRenderer.PhysicsColor;
        set => _debugRenderer.PhysicsColor = value;
    }

    public float DebugLineWidth
    {
        get => _debugRenderer.LineWidth;
        set => _debugRenderer.LineWidth = value;
    }

    public List<IGameObject3d> EntitiesInCullingFrustum { get; } = new();

    public List<IGameObject3d> EntitiesOutsideCullingFrustum { get; } = new();

    public ThreeDLayer(
        RenderContext renderContext,
        ICamera3dService camera3dService,
        IAssetManager assetManager,
        ILightManager lightManager
    ) : base("ThreeD", RenderPriority.ThreeD)
    {
        _renderContext = renderContext;
        _camera3dService = camera3dService;
        _assetManager = assetManager;
        _lightManager = lightManager;

        // Initialize helper renderers
        _shadowRenderer = new ShadowRenderer(renderContext, assetManager);
        _debugRenderer = new DebugRenderer(renderContext, assetManager);
    }

    public override void Initialize()
    {
        _camera3dService.UpdateViewport(_renderContext.GraphicsDevice.Viewport);
        _shadowRenderer.Initialize();
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

        var (dirLights, pointLights, spotLights) = _lightManager.GetActiveLights();
        var shadowLight = _lightManager.ShadowLight is { IsActive: true, CastsShadows: true }
                              ? _lightManager.ShadowLight
                              : null;

        // Render Shadows
        _shadowRenderer.Render(EntitiesInCullingFrustum, shadowLight, cameraPos);

        PrepareMaterialLitShader(cameraPos, dirLights, pointLights, spotLights, shadowLight != null);

        // 3. Draw Opaque Pass (Front-to-Back)
        foreach (var entity in EntitiesInCullingFrustum)
        {
            if (entity is IMaterialCaster materialCaster)
            {
                var materialLit = _assetManager.GetShaderProgram("material_lit");
                var receiveShadows = entity is IShadowReceiver3d shadowReceiver ? shadowReceiver.ReceiveShadows : true;
                ApplyMaterial(materialLit, materialCaster, receiveShadows);
            }

            entity.Draw(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);

            ProcessedEntityCount++;
        }

        // 4. Draw Transparent Pass (Back-to-Front)
        for (var i = EntitiesInCullingFrustum.Count - 1; i >= 0; i--)
        {
            var entity = EntitiesInCullingFrustum[i];

            if (entity is ITransparentRenderable3d transparentEntity)
            {
                transparentEntity.DrawTransparent(gameTime, _renderContext.GraphicsDevice, _camera3dService.ActiveCamera);
            }
        }

        _debugRenderer.Render(EntitiesInCullingFrustum, _camera3dService.ActiveCamera);

        RestoreState();
        EndRenderTimer();

        _renderContext.GraphicsDevice.ResetStates();
    }

    public void Dispose()
    {
        _shadowRenderer.Dispose();
        _debugRenderer.Dispose();
        GC.SuppressFinalize(this);
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

    private void PrepareMaterialLitShader(
        Vector3 cameraPosition,
        DirectionalLight[] dirLights,
        PointLight[] pointLights,
        SpotLight[] spotLights,
        bool hasShadowMap
    )
    {
        var materialLit = _assetManager.GetShaderProgram("material_lit");
        materialLit.Uniforms["uCameraPos"].SetValueVec3(cameraPosition);
        materialLit.Uniforms["uLightView"].SetValueMat4(_shadowRenderer.LightViewMatrix);
        materialLit.Uniforms["uLightProjection"].SetValueMat4(_shadowRenderer.LightProjectionMatrix);

        ApplyLights(materialLit, dirLights, pointLights, spotLights);

        if (hasShadowMap)
        {
            materialLit.Uniforms["uShadowMap"].SetValueTexture(_shadowRenderer.DepthTexture);
        }
        else
        {
            materialLit.Uniforms["uShadowMap"].SetValueTexture(_assetManager.GetWhiteTexture());
        }
    }

    private void ApplyMaterial(
        ShaderProgram materialLit,
        IMaterialCaster caster,
        bool receiveShadows
    )
    {
        var material = caster.Material;

        if (material == null)
        {
            BindDefaultMaterial(materialLit, receiveShadows);

            return;
        }

        var white = _assetManager.GetWhiteTexture();
        var black = _assetManager.GetBlackTexture();

        materialLit.Uniforms["uAlbedoMap"]
                   .SetValueTexture(
                       material.HaveAlbedoTexture ? _assetManager.GetTexture<Texture2D>(material.AlbedoTexture) : white
                   );
        materialLit.Uniforms["uNormalMap"]
                   .SetValueTexture(
                       material.HaveNormalTexture ? _assetManager.GetTexture<Texture2D>(material.NormalTexture) : white
                   );
        materialLit.Uniforms["uRoughnessMap"]
                   .SetValueTexture(
                       material.HaveRoughnessTexture ? _assetManager.GetTexture<Texture2D>(material.RoughnessTexture) : white
                   );
        materialLit.Uniforms["uMetallicMap"]
                   .SetValueTexture(
                       material.HaveMetallicTexture ? _assetManager.GetTexture<Texture2D>(material.MetallicTexture) : white
                   );
        materialLit.Uniforms["uEmissiveMap"]
                   .SetValueTexture(
                       material.HaveEmissiveTexture ? _assetManager.GetTexture<Texture2D>(material.EmissiveTexture) : black
                   );

        materialLit.Uniforms["uTint"].SetValueVec4(material.Tint);
        materialLit.Uniforms["uRoughness"].SetValueFloat(material.Roughness);
        materialLit.Uniforms["uMetallic"].SetValueFloat(material.Metallic);
        materialLit.Uniforms["uEmissiveColor"].SetValueVec3(material.EmissiveColor);
        materialLit.Uniforms["uEmissiveIntensity"].SetValueFloat(material.EmissiveIntensity);

        materialLit.Uniforms["uEnableShadows"].SetValueBool(receiveShadows && material.ReceiveShadows);
    }

    private void BindDefaultMaterial(ShaderProgram materialLit, bool receiveShadows)
    {
        var white = _assetManager.GetWhiteTexture();
        var black = _assetManager.GetBlackTexture();

        materialLit.Uniforms["uAlbedoMap"].SetValueTexture(white);
        materialLit.Uniforms["uNormalMap"].SetValueTexture(white);
        materialLit.Uniforms["uRoughnessMap"].SetValueTexture(white);
        materialLit.Uniforms["uMetallicMap"].SetValueTexture(white);
        materialLit.Uniforms["uEmissiveMap"].SetValueTexture(black);

        materialLit.Uniforms["uTint"].SetValueVec4(Vector4.One);
        materialLit.Uniforms["uRoughness"].SetValueFloat(0.5f);
        materialLit.Uniforms["uMetallic"].SetValueFloat(0.0f);
        materialLit.Uniforms["uEmissiveColor"].SetValueVec3(Vector3.Zero);
        materialLit.Uniforms["uEmissiveIntensity"].SetValueFloat(0.0f);

        materialLit.Uniforms["uEnableShadows"].SetValueBool(receiveShadows);
    }

    private static void ApplyLights(
        ShaderProgram shader,
        DirectionalLight[] dirLights,
        PointLight[] pointLights,
        SpotLight[] spotLights
    )
    {
        shader.Uniforms["uDirectionalLightCount"].SetValueInt(dirLights.Length);

        for (var i = 0; i < dirLights.Length; i++)
        {
            dirLights[i].ApplyToShader(shader, $"uDirectionalLights[{i}]");
        }

        shader.Uniforms["uPointLightCount"].SetValueInt(pointLights.Length);

        for (var i = 0; i < pointLights.Length; i++)
        {
            pointLights[i].ApplyToShader(shader, $"uPointLights[{i}]");
        }

        shader.Uniforms["uSpotLightCount"].SetValueInt(spotLights.Length);

        for (var i = 0; i < spotLights.Length; i++)
        {
            spotLights[i].ApplyToShader(shader, $"uSpotLights[{i}]");
        }
    }
}
