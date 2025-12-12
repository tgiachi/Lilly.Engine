using System.Numerics;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Lights;
using Silk.NET.OpenGL;
using Lilly.Rendering.Core.Types;
using TrippyGL;
using DirectionalLight = Lilly.Rendering.Core.Lights.DirectionalLight;

namespace Lilly.Engine.Pipelines.Renderers;

public class ShadowRenderer : IDisposable
{
    private readonly RenderContext _renderContext;
    private readonly IAssetManager _assetManager;

    private ShaderProgram? _shadowDepthShader;
    private ShadowFramebuffer? _shadowFramebuffer;

    public Matrix4x4 LightViewMatrix { get; private set; } = Matrix4x4.Identity;
    public Matrix4x4 LightProjectionMatrix { get; private set; } = Matrix4x4.Identity;

    public Texture2D DepthTexture => _shadowFramebuffer?.DepthTexture ?? _assetManager.GetWhiteTexture();

    public ShadowRenderer(RenderContext renderContext, IAssetManager assetManager)
    {
        _renderContext = renderContext;
        _assetManager = assetManager;
    }

    public void Initialize()
    {
        _shadowFramebuffer = new ShadowFramebuffer(_renderContext.OpenGl, _renderContext.GraphicsDevice, 2048, 2048);
    }

    public void Render(List<IGameObject3d> entities, DirectionalLight? shadowLight, Vector3 cameraPos)
    {
        if (shadowLight == null || !shadowLight.IsActive || !shadowLight.CastsShadows)
        {
            return;
        }

        _shadowDepthShader ??= _assetManager.GetShaderProgram("shadow_depth");

        BuildLightMatrices(shadowLight, cameraPos);

        var originalViewport = _renderContext.GraphicsDevice.Viewport;

        _shadowFramebuffer?.Bind();
        _renderContext.OpenGl.Clear(ClearBufferMask.DepthBufferBit);
        _renderContext.OpenGl.CullFace(GLEnum.Front);

        foreach (var entity in entities)
        {
            if (entity is not IShadowCaster3d shadowCaster)
            {
                continue;
            }

            shadowCaster.DrawShadow(_shadowDepthShader!, LightViewMatrix, LightProjectionMatrix);
        }

        _renderContext.OpenGl.CullFace(GLEnum.Back);
        _shadowFramebuffer?.Unbind();
        _renderContext.GraphicsDevice.Viewport = originalViewport;
    }

    private void BuildLightMatrices(DirectionalLight light, Vector3 targetCenter, float orthoSize = 50f)
    {
        LightViewMatrix = light.GetShadowViewMatrix(targetCenter, distance: 100f);
        LightProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(
            -orthoSize,
            orthoSize,
            -orthoSize,
            orthoSize,
            0.1f,
            200f
        );
    }

    public void Dispose()
    {
        _shadowFramebuffer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
