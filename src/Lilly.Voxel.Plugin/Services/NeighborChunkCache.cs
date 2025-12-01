using Lilly.Voxel.Plugin.Primitives;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Services;

/// <summary>
/// Caches adjacent chunks for quick boundary queries during meshing.
/// </summary>
internal readonly struct NeighborChunkCache
{
    public NeighborChunkCache(
        ChunkEntity? left,
        ChunkEntity? right,
        ChunkEntity? bottom,
        ChunkEntity? top,
        ChunkEntity? back,
        ChunkEntity? front
    )
    {
        Left = left;
        Right = right;
        Bottom = bottom;
        Top = top;
        Back = back;
        Front = front;
    }

    public ChunkEntity? Left { get; }
    public ChunkEntity? Right { get; }
    public ChunkEntity? Bottom { get; }
    public ChunkEntity? Top { get; }
    public ChunkEntity? Back { get; }
    public ChunkEntity? Front { get; }

    public ChunkEntity? Get(BlockFace face)
        => face switch
        {
            BlockFace.Left   => Left,
            BlockFace.Right  => Right,
            BlockFace.Bottom => Bottom,
            BlockFace.Top    => Top,
            BlockFace.Back   => Back,
            BlockFace.Front  => Front,
            _                => null
        };
}
