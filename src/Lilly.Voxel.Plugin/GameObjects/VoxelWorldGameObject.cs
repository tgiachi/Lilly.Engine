using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Voxel.Plugin.GameObjects.Environment;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Serilog;
using TrippyGL;

namespace Lilly.Voxel.Plugin.GameObjects;

public class VoxelWorldGameObject : BaseGameObject3D
{
    private readonly RainEffectGameObject _rainEffect;
    private readonly SnowEffectGameObject _snowEffect;
    private readonly SkyGameObject _skyGameObject;
    private readonly IChunkGeneratorService _chunkGeneratorService;
    private readonly ILogger _logger = Log.ForContext<VoxelWorldGameObject>();

    private readonly IGameObjectFactory _gameObjectFactory;

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

        _gameObjectFactory = gameObjectFactory;
        _chunkGeneratorService = chunkGeneratorService ?? throw new ArgumentNullException(nameof(chunkGeneratorService));

        _rainEffect = gameObjectFactory.Create<RainEffectGameObject>();
        _snowEffect = gameObjectFactory.Create<SnowEffectGameObject>();
        _skyGameObject = gameObjectFactory.Create<SkyGameObject>();

        AddChild(_skyGameObject);
        AddChild(_rainEffect);
        AddChild(_snowEffect);

        IsRaining = false;
        IsSnowing = false;
    }

    public override void Initialize()
    {
        base.Initialize();

        _ = InitializeTestChunkAsync();
    }

    private async Task InitializeTestChunkAsync()
    {
        try
        {
            _logger.Information("Starting chunk initialization...");
            await _chunkGeneratorService.GenerateInitialChunksAsync();
            _logger.Information("Initial chunks generated");

            // Create chunks in a 3x3 grid around the origin so they're all visible
            var chunkSize = ChunkEntity.Size;
            var chunkIndex = 0;

            // Create a 3x3 grid (9 chunks)
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    if (chunkIndex >= 10)
                        break;

                    var worldX = x * chunkSize;
                    var worldZ = z * chunkSize;
                    var position = new Silk.NET.Maths.Vector3D<float>(worldX, 0, worldZ);

                    _logger.Information("Creating chunk {Index} at position ({X}, 0, {Z})...", chunkIndex, worldX, worldZ);

                    var chunkGameObject = _gameObjectFactory.Create<ChunkGameObject>();
                    AddChild(chunkGameObject);

                    var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(worldX, 0, worldZ);
                    chunkGameObject.SetChunk(chunk);
                    chunkGameObject.Transform.Position = position;

                    _logger.Information(
                        "✓ Chunk {Index} created and added as child at ({X}, 0, {Z}), IsVisible: {Visible}",
                        chunkIndex,
                        worldX,
                        worldZ,
                        chunkGameObject.IsVisible
                    );

                    chunkIndex++;
                }
            }

            // Create the 10th chunk further out
            if (chunkIndex < 10)
            {
                var worldX = 2 * chunkSize;
                var worldZ = 0;
                var position = new Silk.NET.Maths.Vector3D<float>(worldX, 0, worldZ);

                _logger.Information("Creating chunk {Index} at position ({X}, 0, {Z})...", chunkIndex, worldX, worldZ);

                var chunkGameObject = _gameObjectFactory.Create<ChunkGameObject>();
                AddChild(chunkGameObject);

                var chunk = await _chunkGeneratorService.GetChunkByWorldPosition(worldX, 0, worldZ);
                chunkGameObject.SetChunk(chunk);
                chunkGameObject.Transform.Position = position;

                _logger.Information(
                    "✓ Chunk {Index} created and added as child at ({X}, 0, {Z}), IsVisible: {Visible}",
                    chunkIndex,
                    worldX,
                    worldZ,
                    chunkGameObject.IsVisible
                );
            }

            _logger.Information("✅ Generated {Count} test chunks in grid layout", chunkIndex + (chunkIndex < 10 ? 1 : 0));
            _logger.Information("Total children in VoxelWorldGameObject: {ChildCount}", Children.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating test chunks");
        }
    }
}
