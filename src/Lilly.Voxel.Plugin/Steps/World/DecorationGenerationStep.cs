using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Steps.World;

/// <summary>
/// Adds decorative elements such as flora after the terrain has been generated.
/// </summary>
public class DecorationGenerationStep : IGeneratorStep
{
    private readonly ushort _grassId;
    private readonly ushort[] _decorationIds;
    private readonly float _decorationChance;
    private readonly int _maxSlope;

    public DecorationGenerationStep(
        IBlockRegistry blockRegistry,
        float decorationChance = 0.02f,
        IEnumerable<string>? decorationBlockNames = null,
        int maxSlope = 3
    )
    {
        ArgumentNullException.ThrowIfNull(blockRegistry);

        _grassId = Resolve(blockRegistry, "grass", blockRegistry.Air.Id);

        var names = decorationBlockNames?.ToArray() ??
        [
            "flowers1",
            "flowers2",
            "flowers3",
            "flowers4",
            "flowers5",
            "flowers6",
            "flowers7",
            "flowers8",
            "item_1"
        ];

        _decorationIds = names
                         .Select(name => Resolve(blockRegistry, name, blockRegistry.Air.Id))
                         .Where(id => id != blockRegistry.Air.Id)
                         .Distinct()
                         .ToArray();

        _decorationChance = decorationChance;
        _maxSlope = Math.Max(0, maxSlope);
    }

    public string Name => "Decoration";

    public Task ExecuteAsync(IGeneratorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_decorationIds.Length == 0 ||
            !context.CustomData.TryGetValue(GenerationKeys.HeightMap, out var heightMapObj) ||
            heightMapObj is not int[] heightMap)
        {
            return Task.CompletedTask;
        }

        var chunk = context.Chunk;
        var chunkSize = ChunkEntity.Size;
        var chunkBaseY = (int)context.WorldPosition.Y;
        var random = CreateDeterministicRandom(context);

        for (var z = 0; z < chunkSize; z++)
        {
            for (var x = 0; x < chunkSize; x++)
            {
                var surfaceHeight = heightMap[z * chunkSize + x];
                var localSurfaceY = surfaceHeight - chunkBaseY;

                if (localSurfaceY < 0 || localSurfaceY >= ChunkEntity.Height - 1)
                {
                    continue;
                }

                var current = chunk.GetBlock(x, localSurfaceY, z);

                if (current != _grassId || random.NextDouble() > _decorationChance)
                {
                    continue;
                }

                if (!IsSlopeAcceptable(heightMap, chunkSize, x, z, surfaceHeight))
                {
                    continue;
                }

                var aboveBlock = chunk.GetBlock(x, localSurfaceY + 1, z);

                if (aboveBlock != 0)
                {
                    continue;
                }

                var decorationId = _decorationIds[random.Next(_decorationIds.Length)];
                chunk.SetBlock(x, localSurfaceY + 1, z, decorationId);
            }
        }

        chunk.State = ChunkState.Loaded;
        chunk.IsLightingDirty = true;

        return Task.CompletedTask;
    }

    private static Random CreateDeterministicRandom(IGeneratorContext context)
    {
        var coords = context.Chunk.ChunkCoordinates;
        var hash = HashCode.Combine(context.Seed, coords.X, coords.Y, coords.Z, 0xDEC0);

        return new(hash);
    }

    private bool IsSlopeAcceptable(int[] heightMap, int size, int x, int z, int referenceHeight)
    {
        if (_maxSlope <= 0)
        {
            return true;
        }

        for (var dz = -1; dz <= 1; dz++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dz == 0)
                {
                    continue;
                }

                var sampleX = x + dx;
                var sampleZ = z + dz;

                if (sampleX < 0 || sampleX >= size || sampleZ < 0 || sampleZ >= size)
                {
                    continue;
                }

                var neighborHeight = heightMap[sampleZ * size + sampleX];

                if (Math.Abs(neighborHeight - referenceHeight) > _maxSlope)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static ushort Resolve(IBlockRegistry registry, string name, ushort fallback)
    {
        var block = registry.GetByName(name);

        return block == registry.Air ? fallback : block.Id;
    }
}
