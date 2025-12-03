using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Populates the chunk with blocks based on the previously generated height map.
/// </summary>
public class TerrainFillGenerationStep : IGeneratorStep
{
    private readonly ushort _bedrockId;
    private readonly ushort _stoneId;
    private readonly ushort _dirtId;
    private readonly ushort _grassId;
    private readonly ushort _waterId;
    
    // Biome blocks
    private readonly ushort _sandId;
    private readonly ushort _sandstoneId;
    private readonly ushort _snowId;
    private readonly ushort _iceId;

    private readonly float _waterLevel;
    private readonly int _soilDepth;

    public TerrainFillGenerationStep(
        IBlockRegistry blockRegistry,
        float waterLevel = 18f,
        int soilDepth = 3
    )
    {
        ArgumentNullException.ThrowIfNull(blockRegistry);

        _bedrockId = ResolveBlock(blockRegistry, "bedrock", blockRegistry.Air.Id);
        _stoneId = ResolveBlock(blockRegistry, "stone", blockRegistry.Air.Id);
        _dirtId = ResolveBlock(blockRegistry, "dirt", blockRegistry.Air.Id);
        _grassId = ResolveBlock(blockRegistry, "grass", blockRegistry.Air.Id);
        _waterId = ResolveBlock(blockRegistry, "water", blockRegistry.Air.Id);
        
        // Resolve biome blocks with fallbacks
        _sandId = ResolveBlock(blockRegistry, "sand", _dirtId);
        _sandstoneId = ResolveBlock(blockRegistry, "sandstone", _stoneId);
        _snowId = ResolveBlock(blockRegistry, "snow", _grassId);
        _iceId = ResolveBlock(blockRegistry, "ice", _waterId);

        _waterLevel = waterLevel;
        _soilDepth = Math.Max(1, soilDepth);
    }

    public string Name => "Terrain Fill";

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.CustomData.TryGetValue(GenerationKeys.HeightMap, out var heightMapObj) ||
            heightMapObj is not int[] heightMap)
        {
            return Task.CompletedTask;
        }

        // Try get biome maps (optional, fallback to defaults if missing)
        float[]? tempMap = null;
        float[]? humMap = null;
        if (context.CustomData.TryGetValue(GenerationKeys.TemperatureMap, out var tempObj))
            tempMap = tempObj as float[];
        if (context.CustomData.TryGetValue(GenerationKeys.HumidityMap, out var humObj))
            humMap = humObj as float[];

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseY = (int)context.WorldPosition.Y;
        var maxWorldY = chunkBaseY + ChunkEntity.Height - 1;

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var index = z * chunkSize + x;
                var surfaceHeight = heightMap[index];
                
                // Get biome data for this column
                float temp = tempMap != null ? tempMap[index] : 0.5f;
                float hum = humMap != null ? humMap[index] : 0.5f;

                // Determine biome blocks
                ushort topBlock = _grassId;
                ushort subBlock = _dirtId;
                bool isDesert = temp > 0.75f && hum < 0.3f;
                bool isCold = temp < 0.25f;
                bool isBeach = !isDesert && !isCold && surfaceHeight >= _waterLevel - 2 && surfaceHeight <= _waterLevel + 1;

                if (isDesert)
                {
                    topBlock = _sandId;
                    subBlock = _sandstoneId;
                }
                else if (isCold)
                {
                    topBlock = _snowId;
                    subBlock = _dirtId; // Or frozen dirt?
                }
                else if (isBeach)
                {
                    topBlock = _sandId;
                    subBlock = _sandId;
                }

                // Clamp loops to the chunk's vertical span to avoid wasting time when surface is above the chunk
                var maxLocalY = Math.Min(ChunkEntity.Height - 1, (int)(_waterLevel - chunkBaseY));
                var surfaceLocalY = surfaceHeight - chunkBaseY;
                var loopMaxY = Math.Min(ChunkEntity.Height - 1, Math.Max(surfaceLocalY, maxLocalY));

                for (var y = 0; y <= loopMaxY; y++)
                {
                    var worldY = chunkBaseY + y;

                    // Bedrock layer at worldY = -500
                    if (worldY == -500)
                    {
                        chunk.SetBlockFast(x, y, z, _bedrockId);
                        continue;
                    }

                    // Below bedrock is void (air)
                    if (worldY < -500)
                    {
                        continue; // Leave as air
                    }

                    if (worldY > surfaceHeight)
                    {
                        if (worldY <= _waterLevel)
                        {
                            // Freezing water in cold biomes
                            if (isCold && worldY == _waterLevel) // Only top layer freezes? Or all? Let's freeze top.
                            {
                                chunk.SetBlockFast(x, y, z, _iceId);
                            }
                            else
                            {
                                chunk.SetBlockFast(x, y, z, _waterId);
                            }
                        }
                        
                        continue;
                    }

                    var depth = surfaceHeight - worldY;

                    if (depth == 0)
                    {
                        chunk.SetBlockFast(x, y, z, topBlock);
                    }
                    else if (depth <= _soilDepth)
                    {
                        chunk.SetBlockFast(x, y, z, subBlock);
                    }
                    else
                    {
                        chunk.SetBlockFast(x, y, z, _stoneId);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static ushort ResolveBlock(IBlockRegistry registry, string name, ushort fallback)
    {
        var block = registry.GetByName(name);

        return block == registry.Air ? fallback : block.Id;
    }
}
