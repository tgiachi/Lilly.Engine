using System.Numerics;
using FontStashSharp;
using Lilly.Engine.Extensions;
using Lilly.Engine.Fonts;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Engine.Layers;

/// <summary>
/// Provides a render system for batch rendering 2D game objects using sprite batching.
/// </summary>
public class SpriteBatchRenderSystem : BaseRenderLayerSystem<IGameObject2D>, IDisposable
{
    private TextureBatcher _spriteBatcher;
    private readonly RenderContext _context;
    private SimpleShaderProgram _shaderProgram;
    private FontStashRenderer _fontRenderer;

    private readonly IAssetManager _assetManager;

    /// <summary>
    /// This layer processes DrawTexture and DrawText commands.
    /// </summary>
    public override IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; } =
        new HashSet<RenderCommandType>
        {
            RenderCommandType.DrawTexture,
            RenderCommandType.DrawText
        };

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatchRenderSystem" /> class.
    /// </summary>
    /// <param name="context">The render context containing graphics device information.</param>
    public SpriteBatchRenderSystem(RenderContext context, IAssetManager assetManager) : base("SpriteBatch", RenderLayer.UI)
    {
        _context = context;
        _assetManager = assetManager;
    }

/// <summary>
/// Disposes the sprite batcher, shader program, and releases resources.
/// </summary>
public void Dispose()
    {
        _spriteBatcher.Dispose();
        _shaderProgram.Dispose();

        GC.SuppressFinalize(this);
    }

/// <summary>
/// Initializes the sprite batcher and shader program.
/// </summary>
public override void Initialize()
    {
        _spriteBatcher = new(_context.GraphicsDevice);
        _shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(_context.GraphicsDevice, 0, 0, true);
        _shaderProgram.Projection = Matrix4x4.CreateOrthographicOffCenter(
            0,
            _context.GraphicsDevice.Viewport.Width,
            _context.GraphicsDevice.Viewport.Height,
            0,
            0,
            1
        );

        _spriteBatcher.SetShaderProgram(_shaderProgram);
        _fontRenderer = new FontStashRenderer(_context.GraphicsDevice);

        _fontRenderer.SpriteBatcher = _spriteBatcher;
        _fontRenderer.SetShaderProgram(_shaderProgram);

        base.Initialize();
    }

/// <summary>
/// Handles viewport resize events to update the projection matrix.
/// </summary>
/// <param name="width">The new width of the viewport.</param>
/// <param name="height">The new height of the viewport.</param>
public override void OnViewportResize(int width, int height)
    {
        _shaderProgram.Projection = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        base.OnViewportResize(width, height);
    }

/// <summary>
/// Processes render commands for sprite batching.
/// </summary>
/// <param name="renderCommands">The list of render commands to process.</param>
public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        _spriteBatcher.Begin();

        foreach (var command in renderCommands)
        {
            switch (command.CommandType)
            {
                case RenderCommandType.DrawText:
                    DrawText(command.GetPayload<DrawTextPayload>());

                    break;

                case RenderCommandType.DrawTexture:
                    DrawTexture(command.GetPayload<DrawTexturePayload>());

                    break;
            }
        }

        _spriteBatcher.End();
        base.ProcessRenderCommands(ref renderCommands);
    }

    private void DrawText(DrawTextPayload textPayload)
    {
        var font = _assetManager.GetFont<DynamicSpriteFont>(textPayload.FontFamily, textPayload.FontSize);

        if (font == null)
        {
            throw new InvalidOperationException(
                $"Font '{textPayload.FontFamily}' with size {textPayload.FontSize} not found in asset manager."
            );
        }

        var size = font.MeasureString(textPayload.Text, textPayload.Scale.ToNumerics());
        var origin = new Vector2(size.X / 2.0f, size.Y / 2.0f);

        //

        font.DrawText(
            _fontRenderer,
            textPayload.Text,
            textPayload.Position.ToNumerics(),
            new FSColor(textPayload.Color.ToVector4()),
            textPayload.Rotation,
            origin,
            textPayload.Scale.ToNumerics()
        );
    }

    private void DrawTexture(DrawTexturePayload payload)
    {
        var texture = _assetManager.GetTexture<Texture2D>(payload.Texture);

        if (texture == null)
        {
            throw new InvalidOperationException($"Texture '{payload.Texture}' not found in asset manager.");
        }

        // Handle Transform matrix if provided
        if (payload.Transform.HasValue)
        {
            _spriteBatcher.Draw(
                texture,
                payload.Transform.Value.ToNumerics(),
                payload.Source?.ToSystemDrawing(),
                payload.Color.ToVector4(),
                payload.Depth
            );

            return;
        }

        // Handle Destination rectangle if provided
        if (payload.Destination.HasValue)
        {
            _spriteBatcher.Draw(
                texture,
                payload.Destination.Value.ToSystemDrawing(),
                payload.Source?.ToSystemDrawing(),
                payload.Color.ToVector4(),
                payload.Depth
            );

            return;
        }

        // Handle Position with optional rotation, scale, and origin
        if (payload.Position.HasValue)
        {
            var position = payload.Position.Value.ToNumerics();
            var scale = payload.Scale?.ToNumerics() ?? Vector2.One;
            var rotation = payload.Rotation ?? 0.0f;
            var origin = payload.Origin?.ToNumerics() ?? Vector2.Zero;

            _spriteBatcher.Draw(
                texture,
                position,
                payload.Source?.ToSystemDrawing(),
                payload.Color.ToVector4(),
                scale,
                rotation,
                origin,
                payload.Depth
            );

            return;
        }

        // Fallback: draw at origin if no position/transform/destination specified
        _spriteBatcher.Draw(
            texture,
            Vector2.Zero,
            payload.Source?.ToSystemDrawing(),
            payload.Color.ToVector4(),
            Vector2.One,
            0.0f,
            Vector2.Zero,
            payload.Depth
        );
    }
}
