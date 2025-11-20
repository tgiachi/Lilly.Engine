using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Carves caves inside the generated terrain using 3D noise.
/// </summary>
public class CaveGenerationStep : IGeneratorStep
{
    private readonly float _frequency;
    private readonly float _threshold;
    private readonly float _verticalScale;
    private readonly int _seedOffset;
    private readonly int _surfaceBuffer;

    public CaveGenerationStep(
        float frequency = 0.05f,
        float threshold = 0.25f,
        float verticalScale = 0.8f,
        int seedOffset = 8121,
        int surfaceBuffer = 4
    )
    {
        _frequency = frequency;
        _threshold = threshold;
        _verticalScale = verticalScale;
        _seedOffset = seedOffset;
        _surfaceBuffer = Math.Max(0, surfaceBuffer);
    }

    public string Name => "Cave Carving";

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.CustomData.TryGetValue(GenerationKeys.HeightMap, out var heightMapObj) ||
            heightMapObj is not int[] heightMap)
        {
            return Task.CompletedTask;
        }

        var noise = CreateNoise(context.Seed);
        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseX = context.WorldPosition.X;
        var chunkBaseY = context.WorldPosition.Y;
        var chunkBaseZ = context.WorldPosition.Z;

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var surfaceHeight = heightMap[z * chunkSize + x] - _surfaceBuffer;

                for (var y = 1; y < ChunkEntity.Height; y++)
                {
                    var worldY = chunkBaseY + y;

                    if (worldY >= surfaceHeight)
                    {
                        continue;
                    }

                    var worldX = chunkBaseX + x;
                    var worldZ = chunkBaseZ + z;

                    var noiseValue = noise.GetNoise(worldX, worldY * _verticalScale, worldZ);

                    if (noiseValue > _threshold)
                    {
                        chunk.SetBlock(x, y, z, 0);
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
        noise.SetFrequency(_frequency);

        return noise;
    }
}
