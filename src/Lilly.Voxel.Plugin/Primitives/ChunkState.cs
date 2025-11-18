namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents the current state of a chunk in the voxel world.
/// </summary>
public enum ChunkState : byte
{
    /// <summary>
    /// Chunk data is not loaded in memory.
    /// </summary>
    Unloaded = 0,

    /// <summary>
    /// Chunk is in the process of being loaded/generated.
    /// </summary>
    Loading = 1,

    /// <summary>
    /// Chunk is fully loaded and ready for rendering/interaction.
    /// </summary>
    Loaded = 2,

    /// <summary>
    /// Chunk is in the process of being unloaded.
    /// </summary>
    Unloading = 3
}
