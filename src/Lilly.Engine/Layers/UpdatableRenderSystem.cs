using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Layers;

public class UpdatableRenderSystem : BaseRenderLayerSystem<IUpdatable>
{
    public UpdatableRenderSystem() : base("Updateable", RenderLayer.Background) { }

    public override void Update(GameTime gameTime)
    {
        foreach (var gameObject in GetAllTypedGameObjects())
        {
            gameObject.Update(gameTime);
        }

        base.Update(gameTime);
    }
}
