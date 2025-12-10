using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Extensions;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Interfaces.Services;
using Lilly.Physics.Plugin.Data;
using Lilly.Physics.Plugin.Debuggers;
using Lilly.Physics.Plugin.Services;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Physics.Plugin;

public class LillyPhysicPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new(
            "com.tgiachi.lilly.physics",
            "1.0.0",
            "Lilly Physics Plugin",
            "squid"
        );

    public void EngineInitialized(IContainer container) { }

    public void EngineReady(IContainer container) { }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield return gameObjectFactory.Create<PhysicWorldDebugger>();
    }

    public IContainer RegisterModule(IContainer container)
    {
        container.RegisterInstance(new World3dPhysicConfig());
        container.RegisterService<IPhysicWorld3d, PhysicWorld3d>();

        container.RegisterGameObject<PhysicWorldDebugger>();

        return container;
    }

    public string? GetScriptOnLoadFunctionName()
        => null;
}
