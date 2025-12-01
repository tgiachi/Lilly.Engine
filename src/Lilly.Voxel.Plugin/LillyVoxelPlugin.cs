using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Extensions;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.GameObjects;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Json.Contexts;
using Lilly.Voxel.Plugin.Modules;
using Lilly.Voxel.Plugin.Services;
using Lilly.Voxel.Plugin.Steps;
using Lilly.Voxel.Plugin.Steps.World;
using Lilly.Voxel.Plugin.Vertexs;

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

    public void EngineInitialized(IContainer container) { }

    public void EngineReady(IContainer container)
    {
        LoadAssets(container.Resolve<IAssetManager>());
        var blockRegistry = container.Resolve<IBlockRegistry>();
        var chunkGeneratorService = container.Resolve<IChunkGeneratorService>();

        if (_isWorldFlat)
        {
            chunkGeneratorService.AddGeneratorStep(new FlatWorldGenerationStep());
        }
        else
        {
            chunkGeneratorService.AddGeneratorStep(new HeightMapGenerationStep());
            chunkGeneratorService.AddGeneratorStep(new TerrainErosionGenerationStep());
            chunkGeneratorService.AddGeneratorStep(new TerrainFillGenerationStep(blockRegistry));
            chunkGeneratorService.AddGeneratorStep(new CaveGenerationStep());
            chunkGeneratorService.AddGeneratorStep(new DecorationGenerationStep(blockRegistry));
        }
    }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield return gameObjectFactory.Create<SkyGameObject>();
       // yield return gameObjectFactory.Create<RainEffectGameObject>();
    }

    public IContainer RegisterModule(IContainer container)
    {
        container
            .RegisterService<IBlockRegistry, BlockRegistry>()
            .RegisterService<IChunkGeneratorService, ChunkGeneratorService>()
            .RegisterScriptModule<BlockRegistryModule>()
            .RegisterScriptModule<GenerationModule>()
            .RegisterScriptModule<WorldModule>();

        container
            .RegisterGameObject<SnowEffectGameObject>()
            .RegisterGameObject<RainEffectGameObject>()
            .RegisterGameObject<SkyGameObject>()
            ;

        return container;
    }

    private void LoadAssets(IAssetManager assetManager)
    {
        // Chunk rendering shaders
        assetManager.LoadShaderFromResource<ChunkVertex>(
            "chunk_block",
            "Assets/Shaders/Chunks/chunk_block.shader",
            ["aPosition", "aColor", "aTileCoord", "aTileBase", "aTileSize", "aBlockCoord"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadShaderFromResource<ChunkVertex>(
            "chunk_billboard",
            "Assets/Shaders/Chunks/chunk_billboard.shader",
            ["aPosition", "aColor", "aTexCoords", "aTileBase", "aTileSize", "aBlockCoord"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadShaderFromResource<ChunkFluidVertex>(
            "chunk_fluid",
            "Assets/Shaders/Chunks/chunk_fluid.shader",
            ["aPosition", "aColor", "aTexCoord", "aTileBase", "aTileSize", "aDirection", "aTop"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadShaderFromResource<ChunkItemVertex>(
            "chunk_item_billboard",
            "Assets/Shaders/Chunks/chunk_item_billboard.shader",
            ["aPosition", "aColor", "aTexCoord", "aOffset", "aTileBase", "aTileSize"],
            typeof(LillyVoxelPlugin).Assembly
        );

        // Environment shaders
        assetManager.LoadShaderFromResource<PositionVertex>(
            "sky",
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
            "rain",
            "Assets/Shaders/Environment/rain.shader",
            ["aPosition", "aCorner", "aLength", "aAlpha"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadShaderFromResource<CloudsVertex>(
            "clouds_legacy",
            "Assets/Shaders/Environment/clouds.shader",
            ["aPosition", "aNormal"],
            typeof(LillyVoxelPlugin).Assembly
        );

        assetManager.LoadTextureFromResource("snowflake", "Assets/Textures/snowflake.png", typeof(LillyVoxelPlugin).Assembly);
    }
}
