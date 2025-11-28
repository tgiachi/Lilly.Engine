using System.Numerics;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;
using Lilly.Voxel.Plugin.Interfaces.Services;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Contexts;

/// <summary>
/// Concrete implementation of generation context that holds shared data for pipeline steps.
/// </summary>
public class GeneratorContext : IGeneratorContext
{
    /// <inheritdoc />
    public ChunkEntity Chunk { get; set; }

    public Vector3 WorldPosition { get; }

    /// <inheritdoc />
    public FastNoiseLite NoiseGenerator { get; }

    /// <inheritdoc />
    public int Seed { get; }

    /// <inheritdoc />
    public IDictionary<string, object> CustomData { get; }

   // public List<PositionAndSize> CloudAreas { get; } = [];

    private readonly IBlockRegistry _blockRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorContext" /> class.
    /// </summary>
    /// <param name="chunk">The chunk being generated.</param>
    /// <param name="worldPosition">The world position of the chunk.</param>
    /// <param name="noiseGenerator">The noise generator to use.</param>
    /// <param name="seed">The seed for procedural generation.</param>
    public GeneratorContext(
        ChunkEntity chunk,
        IBlockRegistry blockRegistry,
        Vector3 worldPosition,
        FastNoiseLite noiseGenerator,
        int seed
    )
    {
        Chunk = chunk;
        WorldPosition = worldPosition;
        NoiseGenerator = noiseGenerator;
        Seed = seed;
        CustomData = new Dictionary<string, object>();
        _blockRegistry = blockRegistry;
    }

    // public void AddCloudArea(PositionAndSize area)
    // {
    //     CloudAreas.Add(area);
    // }
    //
    // public void AddCloudArea(Vector3 cloudPosition, Vector3 size)
    // {
    //     CloudAreas.Add(new(cloudPosition, size));
    // }
    //
    // public void AddCloudArea(float x, float y, float z, float sizeX, float sizeY, float sizeZ)
    // {
    //     CloudAreas.Add(new(new(x, y, z), new(sizeX, sizeY, sizeZ)));
    // }

    public void AddCustomData(string key, object value)
    {
        CustomData[key] = value;
    }

    public int ChunkHeight()
        => ChunkEntity.Height;

    public ushort? GetBlockIdByName(string name)
    {
        return _blockRegistry.GetByName(name)?.Id;
    }

    public BlockType? GetBlockTypeByName(string name)
    {
        return _blockRegistry.GetByName(name);
    }

    public BlockType? GetBlockTypeById(ushort id)
    {
        return _blockRegistry.GetById(id);
    }

    public uint? GetBlockByName(string name)
    {
        return _blockRegistry.GetByName(name)?.Id;
    }

    public int ChunkSize()
        => ChunkEntity.Size;

    // public void ClearCloudAreas()
    // {
    //     CloudAreas.Clear();
    // }

    /// <summary>
    /// Fills a 3D region with the specified block. Optimized for bulk operations.
    /// </summary>
    public void FillBlocks(int startX, int startY, int startZ, int endX, int endY, int endZ, ushort block)
    {
        // Validate bounds
        if (startX < 0 || endX > ChunkEntity.Size || startX >= endX)
        {
            throw new ArgumentOutOfRangeException(nameof(startX), "Invalid X range");
        }

        if (startY < 0 || endY > ChunkEntity.Height || startY >= endY)
        {
            throw new ArgumentOutOfRangeException(nameof(startY), "Invalid Y range");
        }

        if (startZ < 0 || endZ > ChunkEntity.Size || startZ >= endZ)
        {
            throw new ArgumentOutOfRangeException(nameof(startZ), "Invalid Z range");
        }

        // Fill the region with native C# loops for maximum performance
        for (var x = startX; x < endX; x++)
        {
            for (var z = startZ; z < endZ; z++)
            {
                for (var y = startY; y < endY; y++)
                {
                    Chunk.SetBlock(x, y, z, block);
                }
            }
        }
    }

    /// <summary>
    /// Fills a vertical column from startY to endY at the specified X,Z coordinates.
    /// </summary>
    public void FillColumn(int x, int z, int startY, int endY, ushort block)
    {
        if (x < 0 || x >= ChunkEntity.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X coordinate out of bounds");
        }

        if (z < 0 || z >= ChunkEntity.Size)
        {
            throw new ArgumentOutOfRangeException(nameof(z), "Z coordinate out of bounds");
        }

        if (startY < 0 || endY > ChunkEntity.Height || startY >= endY)
        {
            throw new ArgumentOutOfRangeException(nameof(startY), "Invalid Y range");
        }

        for (var y = startY; y < endY; y++)
        {
            Chunk.SetBlock(x, y, z, block);
        }
    }

    /// <summary>
    /// Fills an entire horizontal layer at the specified Y coordinate.
    /// </summary>
    public void FillLayer(int y, ushort block)
    {
        if (y < 0 || y >= ChunkEntity.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y coordinate out of bounds");
        }

        FillBlocks(0, y, 0, ChunkEntity.Size, y + 1, ChunkEntity.Size, block);
    }

    public ChunkEntity GetChunk()
        => Chunk;

    public FastNoiseLite GetNoise()
        => NoiseGenerator;

    public Vector3 GetWorldPosition()
        => WorldPosition;

    /// <summary>
    /// Sets a single block at the specified coordinates.
    /// Pass null to remove a block (create air/cave).
    /// </summary>
    public void SetBlock(int x, int y, int z, ushort? block)
    {
        // Use default(BlockEntity) for null, which is Air (BlockType.Air = 0)
        Chunk.SetBlock(x, y, z, block ?? 0);
    }

    public override string ToString()
        => $"GeneratorContext(Chunk: {Chunk}, WorldPosition: {WorldPosition}, Seed: {Seed}, CustomDataCount: {CustomData.Count})";
}
