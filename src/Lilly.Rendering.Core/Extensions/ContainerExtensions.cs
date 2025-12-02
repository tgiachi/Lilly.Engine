using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Rendering.Core.Data.Internal;
using Lilly.Rendering.Core.Interfaces.Pipeline;

namespace Lilly.Rendering.Core.Extensions;

/// <summary>
/// Provides extension methods for the dependency injection container related to rendering.
/// </summary>
public static class ContainerExtensions
{
    extension(IContainer container)
    {
        /// <summary>
        /// Registers a render layer with the container.
        /// </summary>
        /// <typeparam name="TRenderLayer">The type of the render layer to register.</typeparam>
        /// <returns>The container for method chaining.</returns>
        public IContainer RegisterRenderLayer<TRenderLayer>() where TRenderLayer : class, IRenderLayer
        {
            container.AddToRegisterTypedList(new RenderLayerRegistration(typeof(TRenderLayer)));

            container.Register<TRenderLayer>(Reuse.Singleton);

            return container;
        }
    }
}
