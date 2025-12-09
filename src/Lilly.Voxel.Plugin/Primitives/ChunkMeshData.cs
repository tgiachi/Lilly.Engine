using Lilly.Voxel.Plugin.Vertexs;

namespace Lilly.Voxel.Plugin.Primitives;

/// <summary>
/// Represents the geometry data generated for a voxel chunk.
/// </summary>
public sealed class ChunkMeshData
{
    /// <summary>
    /// Gets or sets the vertex data for solid block geometry.
    /// </summary>
    public ChunkVertex[] Vertices { get; set; } = Array.Empty<ChunkVertex>();

    /// <summary>
    /// Gets or sets the index data for solid block geometry.
    /// </summary>
    public int[] Indices { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the block mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint TextureHandle { get; set; }

    public string SolidAtlasName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vertex data for billboard geometry (flowers, vegetation).
    /// </summary>
    public ChunkVertex[] BillboardVertices { get; set; } = Array.Empty<ChunkVertex>();

    /// <summary>
    /// Gets or sets the index data for billboard geometry (flowers, vegetation).
    /// </summary>
    public int[] BillboardIndices { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the billboard mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint BillboardTextureHandle { get; set; }

    public string BillboardAtlasName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vertex data for item billboard geometry.
    /// </summary>
    public ChunkItemVertex[] ItemVertices { get; set; } = Array.Empty<ChunkItemVertex>();

    /// <summary>
    /// Gets or sets the index data for item billboard geometry.
    /// </summary>
    public int[] ItemIndices { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the item billboard mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint ItemTextureHandle { get; set; }

    public string ItemAtlasName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vertex data for fluid geometry.
    /// </summary>
    public ChunkFluidVertex[] FluidVertices { get; set; } = Array.Empty<ChunkFluidVertex>();

    /// <summary>
    /// Gets or sets the index data for fluid geometry.
    /// </summary>
    public int[] FluidIndices { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the OpenGL texture handle for the atlas texture used by the fluid mesh.
    /// A value of 0 indicates no texture.
    /// </summary>
    public uint FluidTextureHandle { get; set; }

    public string FluidAtlasName { get; set; } = string.Empty;

    /// <summary>
    /// True when any solid geometry is present.
    /// </summary>
    public bool HasSolidGeometry => Vertices.Length > 0 && Indices.Length > 0;

    /// <summary>
    /// True when any billboard geometry is present.
    /// </summary>
    public bool HasBillboardGeometry => BillboardVertices.Length > 0 && BillboardIndices.Length > 0;

    /// <summary>
    /// True when any item billboard geometry is present.
    /// </summary>
    public bool HasItemGeometry => ItemVertices.Length > 0 && ItemIndices.Length > 0;

    /// <summary>
    /// True when any fluid geometry is present.
    /// </summary>
    public bool HasFluidGeometry => FluidVertices.Length > 0 && FluidIndices.Length > 0;
}
