using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;

namespace Lilly.Engine.Pipelines;

public class SpriteBatcherLayer : BaseRenderLayer<IGameObject>
{
    private readonly RenderContext _context;

    public SpriteBatcherLayer(RenderContext context) : base("SpriteBatcher", RenderPriority.TwoD)
    {
        _context = context;
    }
}
