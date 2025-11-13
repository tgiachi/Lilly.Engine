using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Extensions;

/// <summary>
/// Extension methods for registering game objects in the dependency injection container.
/// </summary>
public static class GameObjectExtensions
{
    /// <summary>
    /// Registers a game object type in the dependency injection container.
    /// </summary>
    /// <typeparam name="TGameObject">The type of game object to register.</typeparam>
    /// <param name="container">The dependency injection container.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterGameObject<TGameObject>(this IContainer container)
        where TGameObject : class, IGameObject
    {
        container.Register<TGameObject>(Reuse.Transient);
        container.AddToRegisterTypedList(new GameObjectRegistration(typeof(TGameObject)));

        return container;
    }
}
