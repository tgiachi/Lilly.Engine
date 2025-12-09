using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Ensures any air pockets below the water level are filled with water (or ice in cold biomes).
/// This runs after carving steps so underwater caves don't remain hollow.
/// </summary>
public class WaterFillGenerationStep : IGeneratorStep
{
    private readonly ushort _waterId;
    private readonly ushort _iceId;
    private readonly float _waterLevel;

    public string Name => "Water Fill";

    public WaterFillGenerationStep(IBlockRegistry blockRegistry, float waterLevel = 18f)
    {
        ArgumentNullException.ThrowIfNull(blockRegistry);

        _waterId = ResolveBlock(blockRegistry, "water", blockRegistry.Air.Id);
        _iceId = ResolveBlock(blockRegistry, "ice", _waterId);
        _waterLevel = waterLevel;
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // No work needed if the chunk is entirely above the water line.
        var chunkBaseY = (int)context.WorldPosition.Y;

        if (chunkBaseY > _waterLevel)
        {
            return Task.CompletedTask;
        }

        float[]? tempMap = null;

        if (context.CustomData.TryGetValue(GenerationKeys.TemperatureMap, out var tempObj))
        {
            tempMap = tempObj as float[];
        }

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var temp = tempMap != null ? tempMap[z * chunkSize + x] : 0.5f;
                var isCold = temp < 0.25f;

                for (var y = 0; y < ChunkEntity.Height; y++)
                {
                    var worldY = chunkBaseY + y;

                    if (worldY > _waterLevel)
                    {
                        continue;
                    }

                    // Leave the void beneath bedrock untouched.
                    if (worldY < -500)
                    {
                        continue;
                    }

                    // Only fill empty space; keep existing blocks (stone, water, decorations, etc.)
                    if (chunk.GetBlockFast(x, y, z) != 0)
                    {
                        continue;
                    }

                    if (isCold && worldY == (int)_waterLevel)
                    {
                        chunk.SetBlockFast(x, y, z, _iceId);
                    }
                    else
                    {
                        chunk.SetBlockFast(x, y, z, _waterId);
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
