using Lilly.Voxel.Plugin.Primitives;

namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Result of a chunk build job: CPU chunk data plus generated mesh and collider data.
/// </summary>
public readonly record struct ChunkBuildResult(ChunkEntity Chunk, ChunkMeshData MeshData, ChunkColliderData ColliderData);
