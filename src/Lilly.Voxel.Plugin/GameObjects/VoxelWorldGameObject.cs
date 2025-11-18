using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.GameObjects.Environment;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Serilog;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class VoxelWorldGameObject : BaseGameObject3D
{
    private readonly RainEffectGameObject _rainEffect;
    private readonly SnowEffectGameObject _snowEffect;
    private readonly SkyGameObject _skyGameObject;
    private readonly ChunkGameObject _testChunk;
    private readonly IChunkGeneratorService _chunkGeneratorService;
    private readonly ILogger _logger = Log.ForContext<VoxelWorldGameObject>();

    public bool IsRaining
    {
        get => _rainEffect.IsVisible;
        set => _rainEffect.IsVisible = value;
    }

    public bool IsSnowing
    {
        get => _snowEffect.IsVisible;
        set => _snowEffect.IsVisible = value;
    }

    public VoxelWorldGameObject(
        IGameObjectFactory gameObjectFactory,
        GraphicsDevice graphicsDevice,
        IChunkGeneratorService chunkGeneratorService
    ) : base(graphicsDevice)
    {
        IgnoreFrustumCulling = true;

        _chunkGeneratorService = chunkGeneratorService ?? throw new ArgumentNullException(nameof(chunkGeneratorService));

        _rainEffect = gameObjectFactory.Create<RainEffectGameObject>();
        _snowEffect = gameObjectFactory.Create<SnowEffectGameObject>();
        _skyGameObject = gameObjectFactory.Create<SkyGameObject>();
        _testChunk = gameObjectFactory.Create<ChunkGameObject>();

        AddChild(_skyGameObject);
        AddChild(_rainEffect);
        AddChild(_snowEffect);
        AddChild(_testChunk);

        IsRaining = false;
        IsSnowing = false;
    }

    public override void Initialize()
    {
        base.Initialize();

        // Generate test chunk asynchronously
        _ = InitializeTestChunkAsync();
    }

    private async Task InitializeTestChunkAsync()
    {
        try
        {
            await _chunkGeneratorService.GenerateInitialChunksAsync();

            _logger.Information("Generating test chunk at position (0, 0, 0)...");

            // Generate chunk using the chunk generator service
            var testChunkEntity = await _chunkGeneratorService.GetChunkByWorldPosition(0, 0, 0);

            if (testChunkEntity != null)
            {
                _testChunk.SetChunk(testChunkEntity);
                _testChunk.Coordinates = new ChunkCoordinates(0, 0, 0);

                _logger.Information("Test chunk loaded and ready for rendering");
            }
            else
            {
                _logger.Warning("Failed to generate test chunk: null result from generator");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating test chunk");
        }
    }
}
