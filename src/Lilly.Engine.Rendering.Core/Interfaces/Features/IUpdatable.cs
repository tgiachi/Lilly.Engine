using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Features;

/// <summary>
/// Defines a game object that can be updated each frame.
/// </summary>
public interface IUpdatable : IGameObject
{
    /// <summary>
    /// Updates the game object's state.
    /// </summary>
    /// <param name="gameTime">The current game time information.</param>
    void Update(GameTime gameTime);
}
