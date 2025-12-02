using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Noise;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Generates simple voxel clouds at a fixed altitude using 3D simplex noise.
/// Mirrors the classic Craft-style clouds: static puffs in the sky layer.
/// </summary>
public class CloudGenerationStep : IGeneratorStep
{
    private readonly IBlockRegistry _blockRegistry;
    private readonly int _cloudMinY;
    private readonly int _cloudMaxY;
    private readonly float _threshold;
    private readonly int _seedOffset;

    public string Name => "Clouds";

    public CloudGenerationStep(
        IBlockRegistry blockRegistry,
        int cloudMinY = 64,
        int cloudMaxY = 71,
        float threshold = 0.65f,
        int seedOffset = 9127
    )
    {
        _blockRegistry = blockRegistry;
        _cloudMinY = cloudMinY;
        _cloudMaxY = cloudMaxY;
        _threshold = threshold;
        _seedOffset = seedOffset;
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cloudBlock = _blockRegistry.GetByName("cloud");

        // If the block isn't registered, skip quickly.
        if (cloudBlock == _blockRegistry.Air)
        {
            return Task.CompletedTask;
        }

        // Skip chunks that don't intersect the cloud layer.
        var chunkBaseY = (int)context.WorldPosition.Y;
        var chunkTopY = chunkBaseY + ChunkEntity.Height - 1;
        if (chunkTopY < _cloudMinY || chunkBaseY > _cloudMaxY)
        {
            return Task.CompletedTask;
        }

        var noise = CreateNoise(context.Seed);
        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseX = (int)context.WorldPosition.X;
        var chunkBaseZ = (int)context.WorldPosition.Z;

        // Clamp local range to only the portion of the cloud band this chunk spans.
        var localStartY = Math.Max(0, _cloudMinY - chunkBaseY);
        var localEndY = Math.Min(ChunkEntity.Height - 1, _cloudMaxY - chunkBaseY);

        for (var y = localStartY; y <= localEndY; y++)
        {
            var worldY = chunkBaseY + y;

            for (var z = 0; z < chunkSize; z++)
            {
                var worldZ = chunkBaseZ + z;

                for (var x = 0; x < chunkSize; x++)
                {
                    var worldX = chunkBaseX + x;
                    var sample = noise.GetNoise(worldX, worldY * 10f, worldZ); // y scaled by 0.1

                    if (sample > _threshold)
                    {
                        chunk.SetBlockFast(x, y, z, cloudBlock.Id);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private FastNoiseLite CreateNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + _seedOffset);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.01f); // x/z freq 0.01; y scaled manually above to 0.1
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetFractalGain(0.5f);
        noise.SetFractalLacunarity(2f);

        return noise;
    }
}
