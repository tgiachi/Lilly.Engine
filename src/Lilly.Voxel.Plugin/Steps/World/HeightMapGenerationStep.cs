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
    private readonly float _continentFrequency;
    private readonly float _mountainFrequency;
    private readonly float _mountainAmplitude;
    private readonly float _mountainThreshold;
    private readonly float _mountainSharpness;
    private readonly int _octaves;
    private readonly float _gain;
    private readonly float _lacunarity;
    private readonly int _seedOffset;

    public HeightMapGenerationStep(
        float baseHeight = 24f,
        float heightVariance = 10f,
        float baseFrequency = 0.009f,
        float continentFrequency = 0.002f,
        float mountainFrequency = 0.018f,
        float mountainAmplitude = 24f,
        float mountainThreshold = 0.35f,
        float mountainSharpness = 2.0f,
        int octaves = 4,
        float gain = 0.45f,
        float lacunarity = 2f,
        int seedOffset = 2718
    )
    {
        _baseHeight = baseHeight;
        _heightVariance = heightVariance;
        _baseFrequency = baseFrequency;
        _continentFrequency = continentFrequency;
        _mountainFrequency = mountainFrequency;
        _mountainAmplitude = mountainAmplitude;
        _mountainThreshold = Math.Clamp(mountainThreshold, 0f, 0.99f);
        _mountainSharpness = Math.Max(0.01f, mountainSharpness);
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
        var tempMap = new float[chunkSize * chunkSize];
        var humMap = new float[chunkSize * chunkSize];

        var baseNoise = CreateBaseNoise(context.Seed);
        var continentNoise = CreateContinentNoise(context.Seed);
        var mountainNoise = CreateMountainNoise(context.Seed);

        // Biome noise generators (very low frequency for large areas)
        var tempNoise = new FastNoiseLite(context.Seed + _seedOffset + 100);
        tempNoise.SetNoiseType(NoiseType.OpenSimplex2);
        tempNoise.SetFrequency(0.005f); // Large biomes

        var humNoise = new FastNoiseLite(context.Seed + _seedOffset + 200);
        humNoise.SetNoiseType(NoiseType.OpenSimplex2);
        humNoise.SetFrequency(0.005f);

        var chunkOriginX = context.WorldPosition.X;
        var chunkOriginZ = context.WorldPosition.Z;

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var worldX = chunkOriginX + x;
                var worldZ = chunkOriginZ + z;

                // 1. Calculate Biome Data
                // Normalize noise from [-1, 1] to [0, 1]
                var temperature = (tempNoise.GetNoise(worldX, worldZ) + 1f) * 0.5f;
                var humidity = (humNoise.GetNoise(worldX, worldZ) + 1f) * 0.5f;

                tempMap[z * chunkSize + x] = temperature;
                humMap[z * chunkSize + x] = humidity;

                // 2. Calculate Height
                var baseValue = baseNoise.GetNoise(worldX, worldZ);
                var continentValue = continentNoise.GetNoise(worldX, worldZ);
                var continentMask = Math.Clamp((continentValue + 1f) * 0.5f, 0f, 1f);
                var mountainMask = GetMountainMask(continentMask);

                var mountainOffset = 0f;

                if (mountainMask > 0.001f)
                {
                    var ridgedValue = 1f - MathF.Abs(mountainNoise.GetNoise(worldX, worldZ));
                    ridgedValue = MathF.Max(0f, ridgedValue * ridgedValue);
                    mountainOffset = ridgedValue * _mountainAmplitude * mountainMask;
                }

                // Apply biome influence to height (optional but good for variety)
                // e.g., Deserts (High Temp, Low Hum) are flatter
                var biomeHeightMod = 1.0f;

                if (temperature > 0.7f && humidity < 0.3f)
                {
                    biomeHeightMod = 0.6f; // Flatten deserts
                }

                var height = _baseHeight +
                             baseValue * _heightVariance * biomeHeightMod +
                             mountainOffset;

                // Allow heights to span multiple vertical chunks; only clamp below to avoid negatives.
                height = MathF.Max(1f, height);

                heightMap[z * chunkSize + x] = (int)MathF.Round(height);
            }
        }

        context.CustomData[GenerationKeys.HeightMap] = heightMap;
        context.CustomData[GenerationKeys.TemperatureMap] = tempMap;
        context.CustomData[GenerationKeys.HumidityMap] = humMap;

        return Task.CompletedTask;
    }

    private FastNoiseLite CreateBaseNoise(int seed)
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

    private FastNoiseLite CreateContinentNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + _seedOffset * 2);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(_continentFrequency);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(Math.Max(2, _octaves - 1));
        noise.SetFractalGain(_gain * 0.85f);
        noise.SetFractalLacunarity(_lacunarity * 0.65f);

        return noise;
    }

    private FastNoiseLite CreateMountainNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + _seedOffset * 3);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(_mountainFrequency);
        noise.SetFractalType(FractalType.Ridged);
        noise.SetFractalOctaves(_octaves + 1);
        noise.SetFractalGain(_gain * 0.9f);
        noise.SetFractalLacunarity(_lacunarity);

        return noise;
    }

    private float GetMountainMask(float continentMask)
    {
        var normalized = MathF.Max(0f, continentMask - _mountainThreshold) / MathF.Max(0.0001f, 1f - _mountainThreshold);

        return MathF.Pow(normalized, _mountainSharpness);
    }
}
