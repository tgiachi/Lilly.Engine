using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;

namespace Lilly.Engine.Rendering.Core.Extensions;

/// <summary>
/// Extension methods for registering render systems in the dependency injection container.
/// </summary>
public static class RegisterRenderSystemExtension
{
    /// <summary>
    /// Registers a render layer system in the container as a singleton and adds it to the render system list.
    /// </summary>
    /// <typeparam name="TRenderLayerSystem">The type of render layer system to register.</typeparam>
    /// <param name="container">The dependency injection container.</param>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterRenderSystem<TRenderLayerSystem>(this IContainer container)
        where TRenderLayerSystem : IRenderLayerSystem
    {
        container.Register<TRenderLayerSystem>(Reuse.Singleton);

        container.AddToRegisterTypedList(new RenderSystemRegistration(typeof(TRenderLayerSystem)));

        return container;
    }
}
