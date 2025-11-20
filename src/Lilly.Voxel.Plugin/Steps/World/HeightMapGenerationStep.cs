using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Generates a height map for the terrain using FastNoiseLite and stores it in the generator context.
/// </summary>
public class HeightMapGenerationStep : IGeneratorStep
{
    private readonly float _baseHeight;
    private readonly float _heightVariance;
    private readonly float _baseFrequency;
    private readonly int _octaves;
    private readonly float _gain;
    private readonly float _lacunarity;
    private readonly int _seedOffset;

    public HeightMapGenerationStep(
        float baseHeight = 58f,
        float heightVariance = 10f,
        float baseFrequency = 0.008f,
        int octaves = 4,
        float gain = 0.45f,
        float lacunarity = 2f,
        int seedOffset = 2718
    )
    {
        _baseHeight = baseHeight;
        _heightVariance = heightVariance;
        _baseFrequency = baseFrequency;
        _octaves = Math.Max(1, octaves);
        _gain = gain;
        _lacunarity = lacunarity;
        _seedOffset = seedOffset;
    }

    public string Name => "Height Map";

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunkSize = ChunkEntity.Size;
        var heightMap = new int[chunkSize * chunkSize];

        var noise = CreateNoise(context.Seed);
        var chunkOriginX = context.WorldPosition.X;
        var chunkOriginZ = context.WorldPosition.Z;

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var worldX = chunkOriginX + x;
                var worldZ = chunkOriginZ + z;

                var noiseValue = noise.GetNoise(worldX, worldZ);
                var height = _baseHeight + noiseValue * _heightVariance;
                height = Math.Clamp(height, 1f, ChunkEntity.Height - 2f);

                heightMap[z * chunkSize + x] = (int)MathF.Round(height);
            }
        }

        context.CustomData[GenerationKeys.HeightMap] = heightMap;

        return Task.CompletedTask;
    }

    private FastNoiseLite CreateNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + _seedOffset);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(_baseFrequency);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(_octaves);
        noise.SetFractalGain(_gain);
        noise.SetFractalLacunarity(_lacunarity);

        return noise;
    }
}
