using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.RenderLayers;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Layers;

/// <summary>
/// Provides a render system for managing and updating game objects that implement IUpdatable.
/// </summary>
public class UpdatableRenderSystem : BaseRenderLayerSystem<IUpdatable>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatableRenderSystem"/> class.
    /// </summary>
    public UpdatableRenderSystem() : base("Updateable", RenderLayer.Background) { }

    /// <summary>
    /// Updates all registered updatable game objects with the current frame timing.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    public override void Update(GameTime gameTime)
    {
        foreach (var gameObject in GetAllTypedGameObjects())
        {
            gameObject.Update(gameTime);
        }

        base.Update(gameTime);
    }
}
