using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Paints the surface layers (Grass, Dirt, Sand, Snow) onto the base Stone terrain.
/// This step should run AFTER terrain shaping and basic cave carving.
/// </summary>
public class SurfacePaintingStep : IGeneratorStep
{
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

    public string Name => "Surface Painting";

    public SurfacePaintingStep(
        IBlockRegistry blockRegistry,
        float waterLevel = 18f,
        int soilDepth = 3
    )
    {
        ArgumentNullException.ThrowIfNull(blockRegistry);

        _stoneId = ResolveBlock(blockRegistry, "stone", blockRegistry.Air.Id);
        _dirtId = ResolveBlock(blockRegistry, "dirt", blockRegistry.Air.Id);
        _grassId = ResolveBlock(blockRegistry, "grass", blockRegistry.Air.Id);
        _waterId = ResolveBlock(blockRegistry, "water", blockRegistry.Air.Id);
        
        _sandId = ResolveBlock(blockRegistry, "sand", _dirtId);
        _sandstoneId = ResolveBlock(blockRegistry, "sandstone", _stoneId);
        _snowId = ResolveBlock(blockRegistry, "snow", _grassId);
        _iceId = ResolveBlock(blockRegistry, "ice", _waterId);

        _waterLevel = waterLevel;
        _soilDepth = Math.Max(1, soilDepth);
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        // Try get biome maps (optional)
        float[]? tempMap = null;
        float[]? humMap = null;
        if (context.CustomData.TryGetValue(GenerationKeys.TemperatureMap, out var tempObj))
            tempMap = tempObj as float[];
        if (context.CustomData.TryGetValue(GenerationKeys.HumidityMap, out var humObj))
            humMap = humObj as float[];

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseY = (int)context.WorldPosition.Y;
        
        // 1. Scan the chunk columns to find surface blocks
        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var index = z * chunkSize + x;
                
                // Get biome info
                float temp = tempMap != null ? tempMap[index] : 0.5f;
                float hum = humMap != null ? humMap[index] : 0.5f;

                ushort topBlock = _grassId;
                ushort subBlock = _dirtId;
                bool isDesert = temp > 0.75f && hum < 0.3f;
                bool isCold = temp < 0.25f;
                bool isBeach = false; // Will calculate based on height later

                if (isDesert)
                {
                    topBlock = _sandId;
                    subBlock = _sandstoneId;
                }
                else if (isCold)
                {
                    topBlock = _snowId;
                    subBlock = _dirtId;
                }

                // Iterate column from top down
                // We only care about modifying the top-most solid blocks into soil/grass
                // Optimization: Start from top of chunk
                int soilRemaining = -1; // -1 means we haven't hit surface yet

                for (var y = ChunkEntity.Height - 1; y >= 0; y--)
                {
                    var blockId = chunk.GetBlockFast(x, y, z);
                    
                    // Skip Air or Water
                    if (blockId == 0 || blockId == _waterId || blockId == _iceId)
                    {
                        soilRemaining = -1; // Reset if we hit air (overhangs)
                        continue;
                    }

                    if (blockId == _stoneId)
                    {
                        int worldY = chunkBaseY + y;

                        // Check for beach (sand near water level)
                        if (!isDesert && !isCold && worldY >= _waterLevel - 2 && worldY <= _waterLevel + 1)
                        {
                            topBlock = _sandId;
                            subBlock = _sandId;
                        }

                        if (soilRemaining == -1)
                        {
                            // Found surface stone
                            soilRemaining = _soilDepth;
                            
                            // If underwater, don't place grass, place dirt/sand/gravel
                            if (worldY < _waterLevel)
                            {
                                chunk.SetBlockFast(x, y, z, subBlock); // Underwater soil
                            }
                            else
                            {
                                chunk.SetBlockFast(x, y, z, topBlock); // Surface grass/sand
                            }
                        }
                        else if (soilRemaining > 0)
                        {
                            // Sub-surface layer
                            chunk.SetBlockFast(x, y, z, subBlock);
                            soilRemaining--;
                        }
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
