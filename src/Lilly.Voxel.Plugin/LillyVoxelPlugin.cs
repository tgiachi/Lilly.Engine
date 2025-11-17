using DryIoc;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Json.Contexts;
using Lilly.Voxel.Plugin.Modules;
using Lilly.Voxel.Plugin.Services;
using Squid.Engine.World.Voxels.Interfaces.Services;

namespace Lilly.Voxel.Plugin;

public class LillyVoxelPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new LillyPluginData("com.lillyengine.voxel", "Lilly Voxel Plugin", "squid", "1.0.0", []);

    public void EngineInitialized(IContainer container) { }

    public IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield break;
    }

    public IContainer RegisterModule(IContainer container)
    {
        JsonUtils.RegisterJsonContext(LillyVoxelJsonContext.Default);

        container.Register<IBlockRegistry, BlockRegistry>();
        container.Register<IChunkGeneratorService, ChunkGeneratorService>();

        container.AddScriptModule<BlockRegistryModule>()
                 .AddScriptModule<GenerationModule>();

        return container;
    }
}
