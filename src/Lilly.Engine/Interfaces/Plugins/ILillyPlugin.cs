using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Interfaces.Plugins;

public interface ILillyPlugin
{
    LillyPluginData LillyData { get; }

    IContainer RegisterModule(IContainer container);

    void EngineInitialized(IContainer container);

    IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory);
}
