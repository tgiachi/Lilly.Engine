using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Defines the vertex layout used by chunk geometry shaders.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ChunkVertex : IVertex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChunkVertex" /> struct.
    /// </summary>
    /// <param name="position">Vertex position.</param>
    /// <param name="color">Packed lighting color, alpha encodes face direction.</param>
    /// <param name="tileCoord">Unscaled tile coordinate for atlas lookup.</param>
    /// <param name="tileBase">Base UV of the atlas region.</param>
    /// <param name="tileSize">Size of the atlas region.</param>
    /// <param name="blockCoord">Block coordinate used for light sampling.</param>
    public ChunkVertex(
        Vector3 position,
        Color4b color,
        Vector2 tileCoord,
        Vector2 tileBase,
        Vector2 tileSize,
        Vector3 blockCoord
    )
    {
        Position = position;
        Color = color;
        TileCoord = tileCoord;
        TileBase = tileBase;
        TileSize = tileSize;
        BlockCoord = blockCoord;
    }

    /// <summary>
    /// Gets or sets the vertex position.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Gets or sets the vertex color (RGBA as normalized bytes).
    /// </summary>
    public Color4b Color;

    /// <summary>
    /// Gets or sets the unscaled tile coordinate used for tiling.
    /// </summary>
    public Vector2 TileCoord;

    /// <summary>
    /// Gets or sets the base UV coordinate of the atlas region.
    /// </summary>
    public Vector2 TileBase;

    /// <summary>
    /// Gets or sets the size of the atlas region.
    /// </summary>
    public Vector2 TileSize;

    /// <summary>
    /// Gets or sets the block coordinate associated with the vertex.
    /// </summary>
    public Vector3 BlockCoord;

    /// <inheritdoc />
    public int AttribDescriptionCount => 6;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new(AttributeType.FloatVec3);

        // Color: vec4 (4 unsigned bytes, normalized)
        descriptions[1] = new(AttributeType.FloatVec4, true, AttributeBaseType.UnsignedByte);

        // TileCoord: vec2 (2 floats)
        descriptions[2] = new(AttributeType.FloatVec2);

        // TileBase: vec2 (2 floats)
        descriptions[3] = new(AttributeType.FloatVec2);

        // TileSize: vec2 (2 floats)
        descriptions[4] = new(AttributeType.FloatVec2);

        // BlockCoord: vec3 (3 floats)
        descriptions[5] = new(AttributeType.FloatVec3);
    }
}
