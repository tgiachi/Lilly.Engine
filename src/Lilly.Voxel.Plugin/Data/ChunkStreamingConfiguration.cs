using System.Numerics;

namespace Lilly.Voxel.Plugin.Data;

/// <summary>
/// Configuration for chunk streaming and generation.
/// </summary>
public class ChunkStreamingConfiguration
{
    /// <summary>
    /// Maximum number of chunks to keep cached in memory.
    /// </summary>
    public int MaxCacheSizeChunks { get; set; } = 256;

    /// <summary>
    /// Horizontal radius in chunks for streaming around the player.
    /// </summary>
    public int HorizontalRadiusChunks { get; set; } = 4;

    /// <summary>
    /// Number of chunks to stream below the player.
    /// </summary>
    public int VerticalBelowChunks { get; set; } = 1;

    /// <summary>
    /// Number of chunks to stream above the player.
    /// </summary>
    public int VerticalAboveChunks { get; set; } = 4;

    /// <summary>
    /// Maximum number of concurrent chunk build jobs.
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 4;

    /// <summary>
    /// Initial chunk radius for world generation at startup.
    /// </summary>
    public int InitialChunkRadius { get; set; } = 3;

    /// <summary>
    /// Minimum chunk layer (Y coordinate) for initial generation.
    /// Start from chunk Y=-512 (to include bedrock at worldY=-500).
    /// </summary>
    public int InitialChunkMinLayer { get; set; } = -16;

    /// <summary>
    /// Maximum chunk layer (Y coordinate) for initial generation.
    /// Up to chunk Y=256.
    /// </summary>
    public int InitialChunkMaxLayer { get; set; } = 8;

    /// <summary>
    /// Radius (in chunks) around the player for which physics colliders are attached.
    /// Keep this smaller than render radius for performance.
    /// </summary>
    public int PhysicsRadiusChunks { get; set; } = 2;

    /// <summary>
    /// Vertical range (below player chunk) for physics colliders.
    /// </summary>
    public int PhysicsVerticalBelowChunks { get; set; } = 1;

    /// <summary>
    /// Vertical range (above player chunk) for physics colliders.
    /// </summary>
    public int PhysicsVerticalAboveChunks { get; set; } = 1;

    /// <summary>
    /// Limits how many chunk colliders are attached per frame to avoid spikes.
    /// </summary>
    public int PhysicsAttachPerFrame { get; set; } = 2;

    /// <summary>
    /// Initial spawn position for chunk generation.
    /// </summary>
    public Vector3 InitialPosition { get; set; } = Vector3.Zero;
}
