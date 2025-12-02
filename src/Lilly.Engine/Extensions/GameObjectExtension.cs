using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data.Internal;
using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Provides extension methods for registering game objects in the dependency injection container.
/// </summary>
/// <summary>
/// Provides extension methods for registering game objects in the DryIoc container.
/// </summary>
public static class GameObjectExtension
{
    /// <summary>
    /// Registers a game object type with the container as transient.
    /// </summary>
    /// <typeparam name="TGameObject">The type of the game object to register.</typeparam>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterGameObject<TGameObject>(this IContainer container)
        where TGameObject : IGameObject
    {
        container.Register<TGameObject>(
            Reuse.Transient,
            setup: Setup.With(
                allowDisposableTransient: true,
                trackDisposableTransient: true
            )
        );

        container.AddToRegisterTypedList(new GameObjectRegistration(typeof(TGameObject)));

        return container;
    }

    /// <summary>
    /// Registers a game object type with the container as transient using reflection.
    /// </summary>
    /// <param name="gameObjectType">The type of the game object to register.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterGameObject(this IContainer container, Type gameObjectType)
    {
        container.Register(
            gameObjectType,
            Reuse.Transient,
            setup: Setup.With(
                allowDisposableTransient: true,
                trackDisposableTransient: true
            )
        );

        container.AddToRegisterTypedList(new GameObjectRegistration(gameObjectType));

        return container;
    }
}

