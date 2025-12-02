using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;

namespace Lilly.Engine.Scenes.Transitions.Interfaces;

/// <summary>
/// Defines the contract for transition effects that render visual effects during scene transitions.
/// </summary>
public interface ITransitionEffect
{
    /// <summary>
    /// Gets the depth value for rendering the effect. Higher values render on top.
    /// </summary>
    float Depth => 0.9f;

    /// <summary>
    /// Renders the transition effect using the sprite batcher.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="progress">The transition progress value from 0 to 1.</param>
    /// <param name="spriteBatcher">The sprite batcher for rendering.</param>
    void Render(GameTime gameTime, float progress, ILillySpriteBatcher spriteBatcher);
}
