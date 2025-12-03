using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Populates the chunk with base terrain (Stone and Water) using 3D density noise.
/// Allows for overhangs, cliffs, and more organic shapes.
/// </summary>
public class TerrainFillGenerationStep : IGeneratorStep
{
    private readonly ushort _stoneId;
    private readonly ushort _waterId;
    private readonly ushort _bedrockId;
    private readonly ushort _iceId;

    private readonly float _waterLevel;
    
    // 3D Noise Settings
    private readonly int _seedOffset;
    private readonly float _noiseFrequency;
    private readonly float _noiseStrength;

    public TerrainFillGenerationStep(
        IBlockRegistry blockRegistry,
        float waterLevel = 18f,
        int seedOffset = 12345,
        float noiseFrequency = 0.02f,
        float noiseStrength = 18.0f // How much the noise distorts the heightmap
    )
    {
        ArgumentNullException.ThrowIfNull(blockRegistry);

        _stoneId = ResolveBlock(blockRegistry, "stone", blockRegistry.Air.Id);
        _waterId = ResolveBlock(blockRegistry, "water", blockRegistry.Air.Id);
        _bedrockId = ResolveBlock(blockRegistry, "bedrock", blockRegistry.Air.Id);
        _iceId = ResolveBlock(blockRegistry, "ice", _waterId);

        _waterLevel = waterLevel;
        _seedOffset = seedOffset;
        _noiseFrequency = noiseFrequency;
        _noiseStrength = noiseStrength;
    }

    public string Name => "Terrain Fill (3D)";

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.CustomData.TryGetValue(GenerationKeys.HeightMap, out var heightMapObj) ||
            heightMapObj is not int[] heightMap)
        {
            return Task.CompletedTask;
        }

        // Prepare 3D Noise
        var noise = new FastNoiseLite(context.Seed + _seedOffset);
        noise.SetNoiseType(NoiseType.OpenSimplex2);
        noise.SetFrequency(_noiseFrequency);
        // Simple ridged fractal for more interesting cliffs
        noise.SetFractalType(FractalType.FBm);
        noise.SetFractalOctaves(2);

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseX = context.WorldPosition.X;
        var chunkBaseY = (int)context.WorldPosition.Y;
        var chunkBaseZ = context.WorldPosition.Z;

        // Try get temperature for ice
        float[]? tempMap = null;
        if (context.CustomData.TryGetValue(GenerationKeys.TemperatureMap, out var tempObj))
            tempMap = tempObj as float[];

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                int index = z * chunkSize + x;
                float baseHeight = heightMap[index];
                float temp = tempMap != null ? tempMap[index] : 0.5f;
                bool isCold = temp < 0.25f;

                for (var y = 0; y < ChunkEntity.Height; y++)
                {
                    var worldY = chunkBaseY + y;

                    // Bedrock at absolute bottom
                    if (worldY == -500)
                    {
                        chunk.SetBlockFast(x, y, z, _bedrockId);
                        continue;
                    }
                    if (worldY < -500) continue; // Void

                    // Density Calculation
                    // Start with basic gradient based on 2D heightmap
                    // If worldY == baseHeight, result is 0. If worldY < baseHeight, result is positive (solid).
                    float density = baseHeight - worldY;

                    // Add 3D noise to distort the terrain
                    // We reduce noise influence as we go deeper so underground is solid
                    // And reduce it high up so mountains taper off
                    float noiseVal = noise.GetNoise(chunkBaseX + x, worldY, chunkBaseZ + z) * _noiseStrength;
                    
                    // Apply noise. 
                    density += noiseVal;

                    if (density > 0)
                    {
                        // Solid Terrain
                        chunk.SetBlockFast(x, y, z, _stoneId);
                    }
                    else
                    {
                        // Air or Water
                        if (worldY <= _waterLevel)
                        {
                            if (isCold && worldY == (int)_waterLevel)
                                chunk.SetBlockFast(x, y, z, _iceId);
                            else
                                chunk.SetBlockFast(x, y, z, _waterId);
                        }
                        // Else leave as 0 (Air)
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

