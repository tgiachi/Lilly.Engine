using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Services;

/// <summary>
/// Manages the lifecycle and distribution of game objects to appropriate rendering layers.
/// This interface decouples scene management from the rendering layer implementation details.
/// </summary>
public interface IGameObjectManager
{

    /// <summary>
    /// Adds a game object to the appropriate rendering layers based on its type.
    /// </summary>
    /// <typeparam name="T">The type of the game object.</typeparam>
    /// <param name="gameObject">The game object to add.</param>
    void AddGameObject<T>(T gameObject) where T : class, IGameObject;

    /// <summary>
    /// Removes a game object from all rendering layers.
    /// </summary>
    /// <typeparam name="T">The type of the game object.</typeparam>
    /// <param name="gameObject">The game object to remove.</param>
    void RemoveGameObject<T>(T gameObject) where T : class;
}
