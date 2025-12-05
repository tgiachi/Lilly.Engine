using System.Collections.Generic;
using System.Numerics;

namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents cheap collision data generated for a chunk.
/// Currently a set of axis-aligned boxes in chunk-local coordinates.
/// </summary>
public sealed class ChunkColliderData
{
    public List<ChunkColliderBox> Boxes { get; } = new();

    public bool IsEmpty => Boxes.Count == 0;
}

/// <summary>
/// Axis-aligned box expressed in chunk-local coordinates.
/// </summary>
/// <param name="Min">Minimum corner (inclusive).</param>
/// <param name="Max">Maximum corner (exclusive).</param>
public readonly record struct ChunkColliderBox(Vector3 Min, Vector3 Max);
