using Lilly.Voxel.Plugin.Primitives.Vertex;

namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents the geometry data generated for a voxel chunk.
/// </summary>
public sealed class ChunkMeshData
{
    /// <summary>
    /// Gets or sets the vertex data for solid block geometry.
    /// </summary>
    public ChunkVertex[] Vertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the index data for solid block geometry.
    /// </summary>
    public int[] Indices { get; set; } = [];

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the block mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint TextureHandle { get; set; }

    /// <summary>
    /// Gets or sets the vertex data for billboard geometry (flowers, vegetation).
    /// </summary>
    public ChunkVertex[] BillboardVertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the index data for billboard geometry (flowers, vegetation).
    /// </summary>
    public int[] BillboardIndices { get; set; } = [];

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the billboard mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint BillboardTextureHandle { get; set; }

    /// <summary>
    /// Gets or sets the vertex data for item billboard geometry.
    /// </summary>
    public ChunkItemVertex[] ItemVertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the index data for item billboard geometry.
    /// </summary>
    public int[] ItemIndices { get; set; } = [];

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the item billboard mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint ItemTextureHandle { get; set; }

    /// <summary>
    /// Gets or sets the vertex data for fluid geometry.
    /// </summary>
    public ChunkFluidVertex[] FluidVertices { get; set; } = [];

    /// <summary>
    /// Gets or sets the index data for fluid geometry.
    /// </summary>
    public int[] FluidIndices { get; set; } = [];

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the fluid mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint FluidTextureHandle { get; set; }
}
