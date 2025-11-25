using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Fonts;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.SpriteBatcher;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Primitives;
using Lilly.Rendering.Core.Types;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Pipelines;

public class SpriteBatcherLayer : BaseRenderLayer<IGameObject2d>, IDisposable
{
    private readonly RenderContext _renderContext;
    private SimpleShaderProgram? _shaderProgram;
    private FontStashRenderer _fontRenderer;
    private TextureBatcher _spriteBatcher;
    private bool _isBatchActive;
    private readonly IAssetManager _assetManager;

    private ILillySpriteBatcher _lillySpriteBatcher;

    public SpriteBatcherLayer(RenderContext renderContext, IAssetManager assetManager) : base(
        "SpriteBatcher",
        RenderPriority.TwoD
    )
    {
        _renderContext = renderContext;
        _assetManager = assetManager;
        _renderContext.Renderer.OnResize += RendererOnOnResize;
    }

    private void RendererOnOnResize(int width, int height)
    {
        _shaderProgram?.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
    }

    public override void Initialize()
    {
        _spriteBatcher = new(_renderContext.GraphicsDevice);
        _shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(_renderContext.GraphicsDevice, 0, 0, true);
        _shaderProgram.Projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            _renderContext.GraphicsDevice.Viewport.Width,
            _renderContext.GraphicsDevice.Viewport.Height,
            0,
            0,
            1
        );

        _spriteBatcher.SetShaderProgram(_shaderProgram);
        _fontRenderer = new(_renderContext.GraphicsDevice);

        _fontRenderer.SpriteBatcher = _spriteBatcher;
        _fontRenderer.SetShaderProgram(_shaderProgram);

        _lillySpriteBatcher = new LillySpriteBatcher(
            _assetManager,
            _spriteBatcher,
            _fontRenderer,
            _renderContext.DpiManager
        );
    }

    public override void Render(GameTime gameTime)
    {
        BeginSpriteBatch();

        foreach (var entity in Entities)
        {
            if (entity.Transform.Size != Vector2.Zero)
            {
                FlushSpriteBatch();
                ApplyScissor(entity.Transform);
            }
            entity.Draw(gameTime, _lillySpriteBatcher);
        }

        EndSpriteBatch();
        base.Render(gameTime);
    }

    private void BeginSpriteBatch()
    {
        if (_isBatchActive)
        {
            return;
        }

        _spriteBatcher.Begin();
        _isBatchActive = true;
    }

    private void EndSpriteBatch()
    {
        if (!_isBatchActive)
        {
            return;
        }

        _spriteBatcher.End();

        _renderContext.GraphicsDevice.ScissorTestEnabled = false;

        _isBatchActive = false;
    }

    private void FlushSpriteBatch()
    {
        if (!_isBatchActive)
        {
            return;
        }

        _spriteBatcher.End();
        _spriteBatcher.Begin();
    }

    private void ApplyScissor(Transform2D tranform)
    {
        var rectangle = new Rectangle<float>(tranform.Position.X, tranform.Position.Y, tranform.Size.X, tranform.Size.Y);
        var width = Math.Max(0, rectangle.Size.X);
        var height = Math.Max(0, rectangle.Size.Y);

        var viewportHeight = _renderContext.GraphicsDevice.Viewport.Height;
        var flippedY = (int)viewportHeight - rectangle.Origin.Y - height;

        _renderContext.GraphicsDevice.ScissorRectangle = new(
            (int)rectangle.Origin.X,
            (int)flippedY,
            (uint)width,
            (uint)height
        );

        _renderContext.GraphicsDevice.ScissorTestEnabled = true;
    }

    public void Dispose()
    {
        _shaderProgram?.Dispose();
        _fontRenderer.Dispose();
        _spriteBatcher.Dispose();
        GC.SuppressFinalize(this);
    }
}
