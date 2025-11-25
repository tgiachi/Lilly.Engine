using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Rendering.Core.Interfaces.Services;

/// <summary>
/// Defines a factory for creating instances of game objects using dependency injection.
/// This factory provides simple, direct object creation without built-in pooling.
/// For performance-critical scenarios requiring manual pooling (e.g., bullets, particles),
/// use IGameObjectPool instead.
/// </summary>
public interface IGameObjectFactory
{
    /// <summary>
    /// Creates an instance of the specified game object type.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object to create.</typeparam>
    /// <returns>A new instance of the game object.</returns>
    TGameObject Create<TGameObject>() where TGameObject : class, IGameObject;

    /// <summary>
    /// Creates an instance of the game object using the specified type.
    /// </summary>
    /// <param name="type">The type of game object to create.</param>
    /// <returns>A new instance of the game object.</returns>
    IGameObject Create(Type type);

    /// <summary>
    /// Determines whether a game object type is registered in the factory.
    /// </summary>
    /// <typeparam name="TGameObject">The game object type to check.</typeparam>
    /// <returns>True if the game object type is registered; otherwise, false.</returns>
    bool IsRegistered<TGameObject>() where TGameObject : class, IGameObject;

    /// <summary>
    /// Determines whether a game object type is registered in the factory.
    /// </summary>
    /// <param name="type">The game object type to check.</param>
    /// <returns>True if the game object type is registered; otherwise, false.</returns>
    bool IsRegistered(Type type);
}
