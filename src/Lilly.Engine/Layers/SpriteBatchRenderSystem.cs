using System.Numerics;
using FontStashSharp;
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
            }
        }

        _spriteBatcher.End();
        base.ProcessRenderCommands(ref renderCommands);
    }

    private void DrawText(DrawTextPayload textPayload)
    {
        var font = _assetManager.GetFont<DynamicSpriteFont>(textPayload.FontFamily, textPayload.FontSize);

        // GetFont already throws if font not found, but check null for safety
        if (font == null)
        {
            throw new InvalidOperationException(
                $"Font '{textPayload.FontFamily}' with size {textPayload.FontSize} not found in asset manager."
            );
        }

        // var scale = new Vector2(2, 2);
        //
        // var size = font.MeasureString(textPayload.Text, scale);
        // var origin = new Vector2(size.X / 2.0f, size.Y / 2.0f);
        //
        // font.DrawText(_fontRenderer, textPayload.Text, new Vector2(400, 400), FSColor.LightCoral, 0, origin, scale);

        font.DrawText(
            _fontRenderer,
            textPayload.Text,
            textPayload.Position.ToNumerics(),
            new FSColor(textPayload.Color.ToVector4()),
            textPayload.Rotation,
            textPayload.Scale.ToNumerics()
        );
    }
}
