using Lilly.Engine.Core.Utils;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Smooths the generated height map to soften sharp transitions.
/// </summary>
public class TerrainErosionGenerationStep : IGeneratorStep
{
    private readonly int _iterations;
    private readonly float _smoothingFactor;

    public TerrainErosionGenerationStep(int iterations = 1, float smoothingFactor = 0.2f)
    {
        _iterations = Math.Max(0, iterations);
        _smoothingFactor = Math.Clamp(smoothingFactor, 0f, 1f);
    }

    public string Name => "Height Smoothing";

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.CustomData.TryGetValue(GenerationKeys.HeightMap, out var heightMapObj) ||
            heightMapObj is not int[] heightMap ||
            _iterations == 0 ||
            _smoothingFactor <= 0f)
        {
            return Task.CompletedTask;
        }

        var size = ChunkEntity.Size;
        var working = new int[heightMap.Length];
        Array.Copy(heightMap, working, heightMap.Length);

        for (var iteration = 0; iteration < _iterations; iteration++)
        {
            for (var z = 0; z < size; z++)
            {
                for (var x = 0; x < size; x++)
                {
                    var total = 0f;
                    var weight = 0f;

                    for (var dz = -1; dz <= 1; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var sampleX = Math.Clamp(x + dx, 0, size - 1);
                            var sampleZ = Math.Clamp(z + dz, 0, size - 1);

                            var influence = dx == 0 && dz == 0 ? 1.5f : 1f;
                            total += working[sampleZ * size + sampleX] * influence;
                            weight += influence;
                        }
                    }

                    var average = total / MathF.Max(1f, weight);
                    var original = working[z * size + x];
                    var smoothed = MathUtils.Lerp(original, average, _smoothingFactor);

                    heightMap[z * size + x] = (int)MathF.Round(MathF.Max(0f, smoothed));
                }
            }

            Array.Copy(heightMap, working, heightMap.Length);
        }

        return Task.CompletedTask;
    }
}
