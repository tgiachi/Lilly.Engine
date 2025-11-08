using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;

namespace Lilly.Engine.Extensions;

public static class RegisterRenderSystemExtension
{
    public static IContainer RegisterRenderSystem<TRenderLayerSystem>(this IContainer container)
        where TRenderLayerSystem : IRenderLayerSystem
    {
        container.Register<TRenderLayerSystem>(Reuse.Singleton);

        container.AddToRegisterTypedList(new RenderSystemRegistration(typeof(TRenderLayerSystem)));

        return container;
    }
}
