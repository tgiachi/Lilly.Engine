using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Engine.Layers;

public class SpriteBatchRenderSystem : BaseRenderLayerSystem<IGameObject2D>, IDisposable
{
    private TextureBatcher _spriteBatcher;
    private readonly RenderContext _context;
    private SimpleShaderProgram _shaderProgram;

    private List<RenderCommand> _renderCommands = new(512);

    public SpriteBatchRenderSystem(RenderContext context) : base("SpriteBatch", RenderLayer.UI)
    {
        _context = context;
    }

    public override void Initialize()
    {
        _spriteBatcher = new TextureBatcher(_context.GraphicsDevice);
        _shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(_context.GraphicsDevice, 0, 0, true);
        _spriteBatcher.SetShaderProgram(_shaderProgram);

        base.Initialize();
    }



    public override void ProcessRenderCommands(ref List<RenderCommand> renderCommands)
    {
        _spriteBatcher.Begin();


        _spriteBatcher.End();
        base.ProcessRenderCommands(ref renderCommands);
    }

    public void Dispose()
    {
        _spriteBatcher.Dispose();
        _shaderProgram.Dispose();

        GC.SuppressFinalize(this);
    }
}
