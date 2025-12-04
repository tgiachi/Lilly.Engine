using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Physics.Plugin.Data;
using Lilly.Physics.Plugin.Interfaces;
using Lilly.Physics.Plugin.Services;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Physics.Plugin;

public class LillyPhysicPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new(
            "com.tgiachi.lilly.physics",
            "Lilly Physics Plugin",
            "Provides 3D physics simulation capabilities to the Lilly Engine.",
            "1.0.0"
        );

    public void EngineInitialized(IContainer container) { }

    public void EngineReady(IContainer container) { }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield break;
    }

    public IContainer RegisterModule(IContainer container)
    {
        container.RegisterInstance(new World3dPhysicConfig());
        container.RegisterService<IPhysicWorld3d, PhysicWorld3d>();

        return container;
    }
}
