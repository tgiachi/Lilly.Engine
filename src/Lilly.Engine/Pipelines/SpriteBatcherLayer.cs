using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Fonts;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.SpriteBatcher;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Layers;
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
    private Rectangle<int>? _currentScissor;

    public SpriteBatcherLayer(RenderContext renderContext, IAssetManager assetManager) : base(
        "SpriteBatcher",
        RenderPriority.TwoD
    )
    {
        _renderContext = renderContext;
        _assetManager = assetManager;
        _renderContext.Renderer.OnResize += RendererOnOnResize;
    }

    public void Dispose()
    {
        _shaderProgram?.Dispose();
        _fontRenderer.Dispose();
        _spriteBatcher.Dispose();
        GC.SuppressFinalize(this);
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
        ProcessedEntityCount = 0;
        StartRenderTimer();
        _renderContext.GraphicsDevice.BlendingEnabled = true;
        _renderContext.GraphicsDevice.BlendState = BlendState.AlphaBlend;
        BeginSpriteBatch();

        foreach (var entity in Entities)
        {
            ProcessedEntityCount++;

            if (entity.Transform.Size != Vector2.Zero)
            {
                ApplyScissorIfChanged(entity);
            }
            else
            {
                DisableScissorIfEnabled();
            }
            entity.Draw(gameTime, _lillySpriteBatcher);
        }

        EndSpriteBatch();
        EndRenderTimer();
        base.Render(gameTime);
    }

    private void ApplyScissorIfChanged(IGameObject2d entity)
    {
        var worldPosition = entity.GetWorldPosition();
        var worldSize = entity.GetWorldSize();

        var width = Math.Max(0, (int)worldSize.X);
        var height = Math.Max(0, (int)worldSize.Y);

        var viewportHeight = _renderContext.GraphicsDevice.Viewport.Height;
        var flippedY = (int)viewportHeight - (int)worldPosition.Y - height;

        var newScissor = new Rectangle<int>((int)worldPosition.X, flippedY, width, height);

        if (_currentScissor.HasValue && _currentScissor.Value == newScissor)
        {
            return;
        }

        FlushSpriteBatch();
        _currentScissor = newScissor;

        _renderContext.GraphicsDevice.ScissorRectangle = new(
            newScissor.Origin.X,
            newScissor.Origin.Y,
            (uint)newScissor.Size.X,
            (uint)newScissor.Size.Y
        );

        _renderContext.GraphicsDevice.ScissorTestEnabled = true;
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

    private void DisableScissorIfEnabled()
    {
        if (!_currentScissor.HasValue)
        {
            return;
        }

        FlushSpriteBatch();
        _currentScissor = null;
        _renderContext.GraphicsDevice.ScissorTestEnabled = false;
    }

    private void EndSpriteBatch()
    {
        if (!_isBatchActive)
        {
            return;
        }

        _spriteBatcher.End();

        _renderContext.GraphicsDevice.ScissorTestEnabled = false;
        _currentScissor = null;

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

    private void RendererOnOnResize(int width, int height)
    {
        _shaderProgram?.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
    }
}
