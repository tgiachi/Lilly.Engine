using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;

namespace Lilly.Engine.Pipelines;

public class UpdateableLayer : BaseRenderLayer<IUpdateble>
{
    public UpdateableLayer() : base("UpdateableLayer", RenderPriority.Background)
    {

    }
}
