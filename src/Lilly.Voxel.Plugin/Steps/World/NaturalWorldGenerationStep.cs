using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Single-step natural terrain generator inspired by MinecraftCloneSilk's WorldNaturalGeneration:
/// - Uses continentalness + amplitude noise to shape large landmasses.
/// - Carves caves with dual OpenSimplex layers.
/// - Paints sand/grass/dirt/stone, places water below sea level, and sprinkles diamonds deep underground.
/// Trees are intentionally omitted.
/// </summary>
public class NaturalWorldGenerationStep : IGeneratorStep
{
    private readonly IBlockRegistry _blockRegistry;

    public string Name => "Natural World Generation";

    public NaturalWorldGenerationStep(IBlockRegistry blockRegistry)
    {
        _blockRegistry = blockRegistry;
    }

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkHeight = ChunkEntity.Height;

        // Resolve block ids up-front.
        var air = _blockRegistry.Air.Id;
        var water = _blockRegistry.GetByName("water").Id;
        var sand = _blockRegistry.GetByName("sand").Id;
        var grass = _blockRegistry.GetByName("grass").Id;
        var stone = _blockRegistry.GetByName("stone").Id;
        var diamond = _blockRegistry.GetByName("diamond").Id;
        var dirt = _blockRegistry.GetByName("dirt").Id;

        // If any critical block is missing, abort gracefully.
        if (water == air || sand == air || grass == air || stone == air || dirt == air)
        {
            return Task.CompletedTask;
        }

        var baseNoise = CreateBaseNoise(context.Seed);
        var amplitudeNoise = CreateAmplitudeNoise(context.Seed);
        var continentalNoise = CreateContinentalNoise(context.Seed);
        var caveNoise = CreateCaveNoise(context.Seed);
        var caveNoise2 = CreateCaveNoise2(context.Seed);
        var humidityNoise = CreateHumidityNoise(context.Seed);
        var diamondNoise = CreateDiamondNoise(context.Seed);

        var originX = (int)context.WorldPosition.X;
        var originY = (int)context.WorldPosition.Y;
        var originZ = (int)context.WorldPosition.Z;

        for (var localZ = 0; localZ < chunkSize; localZ++)
        {
            var worldZ = originZ + localZ;

            for (var localX = 0; localX < chunkSize; localX++)
            {
                var worldX = originX + localX;

                for (var localY = chunkHeight - 1; localY >= 0; localY--)
                {
                    var worldY = originY + localY;
                    var adjustedY = worldY - GetContinentalOffset(continentalNoise, worldX, worldZ);
                    var amplitudeVal = amplitudeNoise.GetNoise(worldX, worldY, worldZ);
                    var thresholdAir = GetThresholdAir(adjustedY, amplitudeVal);
                    var terrainNoise = baseNoise.GetNoise(worldX, adjustedY, worldZ);

                    var isAir = terrainNoise <= thresholdAir;

                    if (isAir)
                    {
                        if (worldY <= 0)
                        {
                            chunk.SetBlockFast(localX, localY, localZ, water);
                        }

                        continue;
                    }

                    // Cave carving near surface.
                    var c1 = caveNoise.GetNoise(worldX, adjustedY, worldZ);
                    var c2 = caveNoise2.GetNoise(worldX + 10000, adjustedY + 10000, worldZ + 10000);

                    if ((terrainNoise - thresholdAir < 0.11f) && c1 >= 0.3f && c2 >= 0.8f)
                    {
                        // Hole close to surface.
                        continue;
                    }

                    // Deep caves and ores.
                    if (terrainNoise - thresholdAir > 0.1f)
                    {
                        var caveAir = c1 >= 0.6f || (c1 >= 0.3f && c2 >= 0.2f);

                        if (caveAir)
                        {
                            continue;
                        }

                        var dia = diamondNoise.GetNoise(worldX, adjustedY, worldZ);
                        chunk.SetBlockFast(localX, localY, localZ, dia <= -0.8f ? diamond : stone);
                        continue;
                    }

                    // Surface painting.
                    if (worldY < -5)
                    {
                        chunk.SetBlockFast(localX, localY, localZ, stone);
                    }
                    else if (worldY < 5)
                    {
                        chunk.SetBlockFast(localX, localY, localZ, sand);
                    }
                    else
                    {
                        var humidity = humidityNoise.GetNoise(worldX, worldZ);
                        var isDesert = IsDesert(humidity, amplitudeVal);

                        if (isDesert)
                        {
                            chunk.SetBlockFast(localX, localY, localZ, sand);
                        }
                        else
                        {
                            // Determine if the block above is air to choose grass vs dirt.
                            var upperAdjustedY = adjustedY + 1;
                            var upperAmplitude = amplitudeNoise.GetNoise(worldX, worldY + 1, worldZ);
                            var upperThreshold = GetThresholdAir(upperAdjustedY, upperAmplitude);
                            var upperIsAir = baseNoise.GetNoise(worldX, upperAdjustedY, worldZ) <= upperThreshold;

                            chunk.SetBlockFast(localX, localY, localZ, upperIsAir ? grass : dirt);
                        }
                    }
                }
            }
        }

        chunk.IsMeshDirty = true;
        chunk.IsLightingDirty = true;

        return Task.CompletedTask;
    }

    private static int GetContinentalOffset(FastNoiseLite continentalNoise, int x, int z)
    {
        var continentalness = continentalNoise.GetNoise(x, z - 200);
        return (int)(Math.Abs(continentalness) * 1000f - 80f);
    }

    private static float GetThresholdAir(int adjustedY, float amplitudeNoise)
    {
        var amplitude = Math.Abs(amplitudeNoise) * 800f + 50f;
        return -0.2f + adjustedY / amplitude;
    }

    private static bool IsDesert(float humidity, float amplitude)
        => humidity < 0f && Math.Abs(amplitude) < 0.5f;

    private static FastNoiseLite CreateBaseNoise(int seed)
    {
        var noise = new FastNoiseLite(seed);
        noise.SetNoiseType(NoiseType.Perlin);
        noise.SetFrequency(0.0015f);
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(6);
        noise.SetFractalLacunarity(2f);
        noise.SetFractalGain(0.9f);
        return noise;
    }

    private static FastNoiseLite CreateAmplitudeNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 7);
        noise.SetNoiseType(NoiseType.Perlin);
        noise.SetFrequency(0.0002f);
        return noise;
    }

    private static FastNoiseLite CreateContinentalNoise(int seed)
    {
        var noise = new FastNoiseLite(seed - 1);
        noise.SetNoiseType(NoiseType.Perlin);
        noise.SetFrequency(0.0002f);
        return noise;
    }

    private static FastNoiseLite CreateCaveNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + 1);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.015f);
        return noise;
    }

    private static FastNoiseLite CreateCaveNoise2(int seed)
    {
        var noise = new FastNoiseLite(seed + 324135);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(0.025f);
        return noise;
    }

    private static FastNoiseLite CreateHumidityNoise(int seed)
    {
        var noise = new FastNoiseLite(seed * 4);
        noise.SetNoiseType(NoiseType.Perlin);
        noise.SetFrequency(0.0005f);
        return noise;
    }

    private static FastNoiseLite CreateDiamondNoise(int seed)
    {
        var noise = new FastNoiseLite(seed * 5);
        noise.SetNoiseType(NoiseType.Perlin);
        noise.SetFrequency(0.2f);
        return noise;
    }
}
