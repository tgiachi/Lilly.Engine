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
using Lilly.Voxel.Plugin.Steps.World;
using Serilog;

namespace Lilly.Voxel.Plugin;

public class LillyVoxelPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new LillyPluginData("com.lillyengine.voxel", "Lilly Voxel Plugin", "squid", "1.0.0", []);

    private readonly ILogger _logger = Log.ForContext<LillyVoxelPlugin>();

    private readonly bool _isWorldFlat ;

    public void EngineInitialized(IContainer container)
    {
        container.Resolve<ChunkLightingService>();
        container.Resolve<ChunkMeshBuilder>();

        LoadShaders(container.Resolve<IAssetManager>());
    }

    public void EngineReady(IContainer container)
    {
        AddGenerationSteps(container.Resolve<IChunkGeneratorService>(), container.Resolve<IBlockRegistry>());
    }

    private void LoadShaders(IAssetManager assetManager)
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
    }

    public IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield return gameObjectFactory.Create<VoxelWorldGameObject>();
    }

    public IContainer RegisterModule(IContainer container)
    {
        JsonUtils.RegisterJsonContext(LillyVoxelJsonContext.Default);

        container.RegisterGameObject<SkyGameObject>()
                 .RegisterGameObject<SnowEffectGameObject>()
                 .RegisterGameObject<RainEffectGameObject>()
                 .RegisterGameObject<VoxelWorldGameObject>()
                 .RegisterGameObject<ChunkGameObject>()
            ;

        container.RegisterService<IBlockRegistry, BlockRegistry>();

        container.RegisterService<IChunkGeneratorService, ChunkGeneratorService>();

        container.RegisterService<ChunkLightingService>();

        container.RegisterService<ChunkMeshBuilder>();

        container
            .AddScriptModule<BlockRegistryModule>()
            .AddScriptModule<GenerationModule>();

        return container;
    }

    private void AddGenerationSteps(IChunkGeneratorService chunkGeneratorService, IBlockRegistry blockRegistry)
    {
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
}
