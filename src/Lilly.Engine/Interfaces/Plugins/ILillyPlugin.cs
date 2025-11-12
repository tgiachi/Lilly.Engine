using DryIoc;
using Lilly.Engine.Data.Plugins;

namespace Lilly.Engine.Interfaces.Plugins;

public interface ILillyPlugin
{
    LillyPluginData LillyData { get; }

    IContainer RegisterModule(IContainer container);

    void EngineInitialized(IContainer container);
}
