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
    
    // Mega Cave settings
    private readonly float _megaFrequency;
    private readonly float _megaThreshold;
    
    private readonly int _seedOffset;
    private readonly int _surfaceBuffer;

    public CaveGenerationStep(
        float frequency = 0.035f, // Slightly wider tunnels
        float threshold = 0.2f,
        float verticalScale = 1.0f,
        float megaFrequency = 0.009f, // Low frequency for massive rooms
        float megaThreshold = 0.55f,  // Higher threshold = rarer but bigger
        int seedOffset = 8121,
        int surfaceBuffer = 6 // Slightly tighter buffer for lower terrain
    )
    {
        _frequency = frequency;
        _threshold = threshold;
        _verticalScale = verticalScale;
        _megaFrequency = megaFrequency;
        _megaThreshold = megaThreshold;
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
        var megaNoise = CreateMegaNoise(context.Seed);
        
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

                var maxLocalY = Math.Min(ChunkEntity.Height - 1, surfaceHeight - chunkBaseY - 1);
                for (var y = 1; y <= maxLocalY; y++)
                {
                    var worldY = chunkBaseY + y;

                    // Optimization: Don't calculate noise above surface buffer
                    if (worldY >= surfaceHeight)
                    {
                        continue;
                    }

                    var worldX = chunkBaseX + x;
                    var worldZ = chunkBaseZ + z;

                    // 1. Check Mega Caves (Big open chambers) - Cheaper check first if possible, or just check
                    // We use simple OpenSimplex2 for big roundish rooms.
                    var megaValue = megaNoise.GetNoise(worldX, worldY * 0.5f, worldZ); // Squashed Y for wider caves
                    
                    if (megaValue > _megaThreshold)
                    {
                        chunk.SetBlockFast(x, y, z, 0);
                        continue; // Skip standard cave check if we already carved
                    }

                    // 2. Check Standard Caves (Tunnels)
                    // We check depth to make tunnels slightly more common deeper down
                    var noiseValue = noise.GetNoise(worldX, worldY * _verticalScale, worldZ);

                    if (noiseValue > _threshold)
                    {
                        chunk.SetBlockFast(x, y, z, 0);
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
        noise.SetFractalType(FractalType.FBm); // Fractal adds detail/roughness to tunnels
        noise.SetFractalOctaves(1); 

        return noise;
    }

    private FastNoiseLite CreateMegaNoise(int seed)
    {
        var noise = new FastNoiseLite(seed + _seedOffset + 1337);
        noise.SetNoiseType(NoiseType.OpenSimplex2); // Smooth round shapes
        noise.SetFrequency(_megaFrequency);
        // No fractal for mega caves, we want smooth large chambers
        return noise;
    }
}
