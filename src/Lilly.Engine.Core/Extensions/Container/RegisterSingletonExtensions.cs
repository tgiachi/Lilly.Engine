using DryIoc;

namespace Lilly.Engine.Core.Extensions.Container;

/// <summary>
/// Extension methods for registering services as singletons in the dependency injection container.
/// </summary>
public static class RegisterSingletonExtensions
{
    /// <param name="container">The dependency injection container.</param>
    extension(IContainer container)
    {
        /// <summary>
        /// Registers a service with its implementation as a singleton in the container.
        /// </summary>
        /// <typeparam name="TService">The service interface type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <returns>The container for method chaining.</returns>
        public IContainer RegisterService<TService, TImplementation>()
            where TImplementation : TService
        {
            container.Register<TService, TImplementation>(
                Reuse.Singleton,
                setup: Setup.With(
                    allowDisposableTransient: true,
                    trackDisposableTransient: true
                )
            );

            return container;
        }

        public IContainer RegisterService<TService>()
        {
            container.Register<TService>(
                Reuse.Singleton,
                setup: Setup.With(
                    allowDisposableTransient: true,
                    trackDisposableTransient: true
                )
            );

            return container;
        }
    }
}
