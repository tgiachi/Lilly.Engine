using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Services;

/// <summary>
/// Defines an optional pooling service for developers who want to manually manage object pooling.
/// This is intended for performance-critical scenarios with high object creation/destruction rates.
/// Example use cases: bullet pools, particle effect pools, enemy pools in intensive gameplay.
/// </summary>
public interface IGameObjectPool
{
    /// <summary>
    /// Clears all pooled objects of the specified type.
    /// </summary>
    /// <typeparam name="TGameObject">The game object type to clear from the pool.</typeparam>
    void Clear<TGameObject>() where TGameObject : class, IGameObject;

    /// <summary>
    /// Clears all pooled objects of the specified type.
    /// </summary>
    /// <param name="type">The game object type to clear from the pool.</param>
    void Clear(Type type);

    /// <summary>
    /// Clears all pooled objects of all types.
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Gets or creates a game object from the pool. If no pooled instance is available, a new one is created.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object to get from the pool.</typeparam>
    /// <returns>A game object instance from the pool or newly created.</returns>
    TGameObject GetOrCreate<TGameObject>() where TGameObject : class, IGameObject;

    /// <summary>
    /// Gets or creates a game object from the pool using the specified type. If no pooled instance is available, a new one is
    /// created.
    /// </summary>
    /// <param name="type">The game object type to get from the pool.</param>
    /// <returns>A game object instance from the pool or newly created.</returns>
    IGameObject GetOrCreate(Type type);
}
