using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
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

    private List<RenderCommand> _renderCommands = new(512);

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteBatchRenderSystem" /> class.
    /// </summary>
    /// <param name="context">The render context containing graphics device information.</param>
    public SpriteBatchRenderSystem(RenderContext context) : base("SpriteBatch", RenderLayer.UI)
        => _context = context;

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
        _spriteBatcher.SetShaderProgram(_shaderProgram);

        base.Initialize();
    }

    /// <summary>
    /// Processes render commands for sprite batching.
    /// </summary>
    /// <param name="renderCommands">The list of render commands to process.</param>
    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        _spriteBatcher.Begin();

        _spriteBatcher.End();
        base.ProcessRenderCommands(ref renderCommands);
    }
}
