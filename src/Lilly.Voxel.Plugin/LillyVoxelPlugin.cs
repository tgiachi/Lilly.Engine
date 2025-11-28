using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Json.Contexts;
using Lilly.Voxel.Plugin.Modules;
using Lilly.Voxel.Plugin.Services;
using Lilly.Voxel.Plugin.Steps;

namespace Lilly.Voxel.Plugin;

public class LillyVoxelPlugin : ILillyPlugin
{
    private bool _isWorldFlat = true;

    public LillyPluginData LillyData
        => new LillyPluginData(
            "com.tgiachi.lilly.voxel",
            "Lilly Voxel Plugin",
            "0.1.0",
            "squid",
            "com.tgiachi.lilly.gameobjects"
        );

    public LillyVoxelPlugin()
    {
        JsonUtils.RegisterJsonContext(LillyVoxelJsonContext.Default);
    }

    public void EngineInitialized(IContainer container)
    {
        container
            .RegisterService<IBlockRegistry, BlockRegistry>()
            .RegisterService<IChunkGeneratorService, ChunkGeneratorService>()
            .RegisterScriptModule<BlockRegistryModule>()
            .RegisterScriptModule<GenerationModule>()
            .RegisterScriptModule<WorldModule>();
    }

    public void EngineReady(IContainer container)
    {
        if (_isWorldFlat)
        {
            var chunkGenerator = container.Resolve<IChunkGeneratorService>();
            chunkGenerator.AddGeneratorStep(new FlatWorldGenerationStep());
        }
        else
        {
            
        }

    }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield break;
    }

    public IContainer RegisterModule(IContainer container)
    {
        return container;
    }
}
