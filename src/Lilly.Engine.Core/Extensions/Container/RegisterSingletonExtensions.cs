
using DryIoc;

namespace Lilly.Engine.Core.Extensions.Container;

public static class RegisterSingletonExtensions
{
    public static IContainer RegisterService<TService, TImplementation>(this IContainer container) where TImplementation : TService
    {
        container.Register<TService,TImplementation>(Reuse.Singleton);

        return container;
    }

}
