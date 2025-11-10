using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Services;

/// <summary>
/// Defines a factory for creating instances of game objects.
/// </summary>
public interface IGameObjectFactory
{
    /// <summary>
    /// Creates an instance of the specified game object type.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object to create.</typeparam>
    /// <returns>A new instance of the game object.</returns>
    TGameObject CreateGameObject<TGameObject>() where TGameObject : class, IGameObject;

    /// <summary>
    /// Creates an instance of the game object using the specified type.
    /// </summary>
    /// <param name="type">The type of game object to create.</param>
    /// <returns>A new instance of the game object.</returns>
    IGameObject CreateGameObject(Type type);
}
