using DryIoc;

namespace Lilly.Engine.Core.Extensions.Container;

/// <summary>
/// Extension methods for registering services as singletons in the dependency injection container.
/// </summary>
public static class RegisterSingletonExtensions
{
    /// <summary>
    /// Registers a service with its implementation as a singleton in the container.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="container">The dependency injection container.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterService<TService, TImplementation>(this IContainer container)
        where TImplementation : TService
    {
        container.Register<TService, TImplementation>(Reuse.Singleton);

        return container;
    }
}
