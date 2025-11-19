using DryIoc;
using Lilly.Engine.Core.Data.Services;

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
        public IContainer RegisterService<TService, TImplementation>(bool autoStart = false)
            where TImplementation : TService
        {
            container.Register<TService, TImplementation>(
                Reuse.Singleton,
                setup: Setup.With(
                    allowDisposableTransient: true,
                    trackDisposableTransient: true
                )
            );

            if (autoStart)
            {
                container.AddToRegisterTypedList(new AutostartRegistration(typeof(TService)));
            }

            return container;
        }

        public IContainer RegisterService<TService>()
        {
            return container.RegisterService<TService, TService>();
        }
    }
}
