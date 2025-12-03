using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Interfaces.Services;

/// <summary>
/// Exposes access to currently cached/active chunks.
/// </summary>
public interface IChunkProvider
{
    IEnumerable<ChunkEntity> GetActiveChunks();
}
