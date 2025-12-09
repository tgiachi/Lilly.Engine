using DryIoc;
using Lilly.Engine.Core.Data.Services;

namespace Lilly.Engine.Core.Extensions.Container;

/// <summary>
/// Provides extension methods for registering services as singletons in the DryIoc dependency injection container.
/// </summary>
public static class RegisterSingletonExtensions
{
    /// <summary>
    /// Registers a service with its implementation as a singleton in the container.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="autoStart">Whether to auto-start the service.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterService<TService, TImplementation>(this IContainer container, bool autoStart = false)
        where TImplementation : TService
    {
        container.Register<TService, TImplementation>(
            Reuse.Singleton,
            setup: Setup.With(
                allowDisposableTransient: true,
                trackDisposableTransient: true
            )
        );

        container.AddToRegisterTypedList(new AutostartRegistration(typeof(TService), autoStart));

        return container;
    }

    /// <summary>
    /// Registers a service with itself as a singleton when the service and implementation are the same type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterService<TService>(this IContainer container)
        => container.RegisterService<TService, TService>();
}
