using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Interfaces.Plugins;

public interface ILillyPlugin
{
    LillyPluginData LillyData { get; }

    void EngineInitialized(IContainer container);

    void EngineReady(IContainer container);

    IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory);

    IContainer RegisterModule(IContainer container);
}
