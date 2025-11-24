using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Interfaces.Plugins;

namespace Lilly.Engine.Extensions;

public static class PluginExtension
{
    [RequiresUnreferencedCode("Plugin registration requires reflection on assemblies which may not be trimmed.")]
    public static IContainer RegisterPlugin(this IContainer container, Assembly assembly)
    {
        var pluginTypes = assembly.GetTypes()
                                  .Where(t => typeof(ILillyPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var pluginType in pluginTypes)
        {
            container.AddToRegisterTypedList(new EnginePluginRegistration(assembly, pluginType));
            container.Register(pluginType, Reuse.Singleton);
        }

        return container;
    }
}
