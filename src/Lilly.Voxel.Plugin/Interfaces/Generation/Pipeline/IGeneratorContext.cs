using System.Numerics;
using Lilly.Voxel.Plugin.Noise;
using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Interfaces.Generation.Pipeline;

/// <summary>
/// Provides shared context data for all generation steps in the chunk pipeline.
/// </summary>
public interface IGeneratorContext
{
    /// <summary>
    /// Gets the chunk being generated.
    /// </summary>
    ChunkEntity Chunk { get; set; }

    /// <summary>
    /// Gets the world position for the chunk.
    /// </summary>
    Vector3 WorldPosition { get; }

    /// <summary>
    /// Gets the noise generator used for terrain generation.
    /// </summary>
    FastNoiseLite NoiseGenerator { get; }

    /// <summary>
    /// Gets the seed used for procedural generation.
    /// </summary>
    int Seed { get; }

    /// <summary>
    /// Gets or sets custom data that can be shared between pipeline steps.
    /// </summary>
    IDictionary<string, object> CustomData { get; }

    int ChunkSize();

    int ChunkHeight();

    ushort? GetBlockIdByName(string name);

    BlockType? GetBlockTypeByName(string name);

    BlockType? GetBlockTypeById(ushort id);


    ChunkEntity GetChunk();

    Vector3 GetWorldPosition();

    FastNoiseLite GetNoise();

    /// <summary>
    /// Fills a 3D region with the specified block. Much faster than setting blocks individually from scripts.
    /// </summary>
    /// <param name="startX">Starting X coordinate (inclusive).</param>
    /// <param name="startY">Starting Y coordinate (inclusive).</param>
    /// <param name="startZ">Starting Z coordinate (inclusive).</param>
    /// <param name="endX">Ending X coordinate (exclusive).</param>
    /// <param name="endY">Ending Y coordinate (exclusive).</param>
    /// <param name="endZ">Ending Z coordinate (exclusive).</param>
    /// <param name="block">The block to fill the region with.</param>
    void FillBlocks(int startX, int startY, int startZ, int endX, int endY, int endZ, ushort block);

    /// <summary>
    /// Fills an entire horizontal layer at the specified Y coordinate with the given block.
    /// </summary>
    /// <param name="y">The Y coordinate of the layer.</param>
    /// <param name="block">The block to fill the layer with.</param>
    void FillLayer(int y, ushort block);

    /// <summary>
    /// Fills a vertical column from startY to endY at the specified X,Z coordinates.
    /// </summary>
    /// <param name="x">The X coordinate of the column.</param>
    /// <param name="z">The Z coordinate of the column.</param>
    /// <param name="startY">Starting Y coordinate (inclusive).</param>
    /// <param name="endY">Ending Y coordinate (exclusive).</param>
    /// <param name="block">The block to fill the column with.</param>
    void FillColumn(int x, int z, int startY, int endY, ushort block);

    /// <summary>
    /// Sets a single block at the specified coordinates. Optimized for script access.
    /// Pass null to remove a block (create air/cave).
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    /// <param name="block">The block to set, or null to remove the block.</param>
    void SetBlock(int x, int y, int z, ushort? block);


    void AddCustomData(string key, object value);

    //void AddCloudArea(PositionAndSize area);

    // void ClearCloudAreas();
    //
    // void AddCloudArea(Vector3 cloudPosition, Vector3 size);
    //
    // void AddCloudArea(float x, float y, float z, float sizeX, float sizeY, float sizeZ);
}
