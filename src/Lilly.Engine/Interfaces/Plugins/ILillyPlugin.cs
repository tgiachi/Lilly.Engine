using System.ComponentModel;
using Lilly.Engine.Data.Plugins;

namespace Lilly.Engine.Interfaces.Plugins;

public interface ILillyPlugin
{
    LillyPluginData LillyData { get; }

    void EngineInitialized(IContainer container);

    void EngineReady(IContainer container);

    // IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory);

    IContainer RegisterModule(IContainer container);
}
