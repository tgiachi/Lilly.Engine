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
    private readonly float _waterLevel;
    private readonly int _soilDepth;

    public TerrainFillGenerationStep(
        IBlockRegistry blockRegistry,
        float waterLevel = 38f,
        int soilDepth = 3
    )
    {
        ArgumentNullException.ThrowIfNull(blockRegistry);

        _bedrockId = ResolveBlock(blockRegistry, "bedrock", blockRegistry.Air.Id);
        _stoneId = ResolveBlock(blockRegistry, "stone", blockRegistry.Air.Id);
        _dirtId = ResolveBlock(blockRegistry, "dirt", blockRegistry.Air.Id);
        _grassId = ResolveBlock(blockRegistry, "grass", blockRegistry.Air.Id);
        _waterId = ResolveBlock(blockRegistry, "water", blockRegistry.Air.Id);
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

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseY = (int)context.WorldPosition.Y;

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var surfaceHeight = heightMap[z * chunkSize + x];

                for (var y = 0; y < ChunkEntity.Height; y++)
                {
                    var worldY = chunkBaseY + y;

                    if (worldY == 0)
                    {
                        chunk.SetBlockFast(x, y, z, _bedrockId);
                        continue;
                    }

                    if (worldY > surfaceHeight)
                    {
                        if (worldY <= _waterLevel)
                        {
                            chunk.SetBlockFast(x, y, z, _waterId);
                        }

                        continue;
                    }

                    var depth = surfaceHeight - worldY;

                    if (depth == 0)
                    {
                        chunk.SetBlockFast(x, y, z, _grassId);
                    }
                    else if (depth <= _soilDepth)
                    {
                        chunk.SetBlockFast(x, y, z, _dirtId);
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
