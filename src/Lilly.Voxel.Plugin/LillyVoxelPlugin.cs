using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Extensions;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Shaders;
using Lilly.Voxel.Plugin.GameObjects;
using Lilly.Voxel.Plugin.GameObjects.Environment;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Json.Contexts;
using Lilly.Voxel.Plugin.Modules;
using Lilly.Voxel.Plugin.Primitives.Vertex;
using Lilly.Voxel.Plugin.Services;
using Lilly.Voxel.Plugin.Steps;
using Serilog;

namespace Lilly.Voxel.Plugin;

public class LillyVoxelPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new LillyPluginData("com.lillyengine.voxel", "Lilly Voxel Plugin", "squid", "1.0.0", []);

    private readonly ILogger _logger = Log.ForContext<LillyVoxelPlugin>();

    public void EngineInitialized(IContainer container)
    {
        var assetManager = container.Resolve<IAssetManager>();

        assetManager.LoadShaderFromResource<PositionVertex>(
            "dynamicSky",
            "Assets/Shaders/Environment/dynamic_sky.shader",
            ["aPosition"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadShaderFromResource<SnowVertex>(
            "snow",
            "Assets/Shaders/Environment/snow.shader",
            ["aPosition", "aCorner", "aSize", "aAlpha"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadShaderFromResource<RainVertex>(
            "rain_legacy",
            "Assets/Shaders/Environment/rain_legacy.shader",
            ["aPosition", "aCorner", "aLength", "aAlpha"],
            typeof(LillyVoxelPlugin).Assembly
        );

        var generatorService = container.Resolve<IChunkGeneratorService>();

        var jobSystem = container.Resolve<IJobSystemService>();

        generatorService.AddGeneratorStep(new FlatWorldGenerationStep());

        jobSystem.ExecuteAsync(
            "LillyVoxelPlugin: Load Default Generators",
            async () =>
            {
                await generatorService.GenerateInitialChunksAsync();
                _logger.Information("Default chunk generators loaded.");
            }
        );
    }

    public IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        // yield return gameObjectFactory.Create<SkyGameObject>();
        // yield return gameObjectFactory.Create<SnowEffectGameObject>();
        // yield return gameObjectFactory.Create<RainEffectGameObject>();
        yield return gameObjectFactory.Create<VoxelWorldGameObject>();
    }

    public IContainer RegisterModule(IContainer container)
    {
        JsonUtils.RegisterJsonContext(LillyVoxelJsonContext.Default);

        container.RegisterGameObject<SkyGameObject>()
                 .RegisterGameObject<SnowEffectGameObject>()
                 .RegisterGameObject<RainEffectGameObject>()
                 .RegisterGameObject<VoxelWorldGameObject>()
            ;

        container.RegisterService<IBlockRegistry, BlockRegistry>();

        container.RegisterService<IChunkGeneratorService, ChunkGeneratorService>();

        container
            .AddScriptModule<BlockRegistryModule>()
            .AddScriptModule<GenerationModule>();

        return container;
    }
}
