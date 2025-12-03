using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Rendering.Core.Interfaces.Services;

/// <summary>
/// Manages the lifecycle and registration of game objects in the rendering system.
/// Provides methods to create, add, remove, and retrieve game objects.
/// </summary>
public interface IGameObjectManager
{
    /// <summary>
    /// Adds an existing game object to the manager's collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the game object, must implement IGameObject.</typeparam>
    /// <param name="entity">The game object instance to add.</param>
    void AddGameObject<TEntity>(TEntity entity) where TEntity : IGameObject;

    /// <summary>
    /// Creates a new instance of a game object and adds it to the manager.
    /// </summary>
    /// <typeparam name="TEntity">The type of the game object to create, must implement IGameObject and be a class.</typeparam>
    /// <returns>The newly created game object instance.</returns>
    TEntity CreateGameObject<TEntity>() where TEntity : class, IGameObject;

    /// <summary>
    /// Removes a game object from the manager's collection.
    /// </summary>
    /// <typeparam name="TEntity">The type of the game object, must implement IGameObject.</typeparam>
    /// <param name="entity">The game object instance to remove.</param>
    void RemoveGameObject<TEntity>(TEntity entity) where TEntity : IGameObject;

    /// <summary>
    /// Retrieves a game object of the specified type from the manager.
    /// </summary>
    /// <typeparam name="TGameObject">The type of the game object to retrieve, must implement IGameObject.</typeparam>
    /// <returns>The game object instance if found, otherwise null.</returns>
    TGameObject? GetGameObjectOfType<TGameObject>() where TGameObject : IGameObject;
}
