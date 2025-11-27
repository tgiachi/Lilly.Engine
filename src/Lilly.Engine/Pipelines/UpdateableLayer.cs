using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Layers;
using Lilly.Rendering.Core.Types;

namespace Lilly.Engine.Pipelines;

/// <summary>
/// A render layer that updates all entities that implement IUpdateble.
/// </summary>
public class UpdateableLayer : BaseRenderLayer<IUpdateble>
{
    /// <summary>
    /// Initializes a new instance of the UpdateableLayer class.
    /// </summary>
    public UpdateableLayer() : base("UpdateableLayer", RenderPriority.Background)
    {

    }

    /// <summary>
    /// Updates all entities in the layer.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public override void Update(GameTime gameTime)
    {
        ProcessedEntityCount = 0;
        StartUpdateTimer();
        foreach (var entity in Entities)
        {
            ProcessedEntityCount++;
            entity.Update(gameTime);
        }

        base.Update(gameTime);
        EndUpdateTimer();
    }
}
