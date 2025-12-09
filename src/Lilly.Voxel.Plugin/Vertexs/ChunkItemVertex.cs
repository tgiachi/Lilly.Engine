using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Defines the vertex layout used by item billboard shaders.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ChunkItemVertex : IVertex
{
    public ChunkItemVertex(
        Vector3 position,
        Color4b color,
        Vector2 texCoord,
        Vector2 offset,
        Vector2 tileBase,
        Vector2 tileSize
    )
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
        Offset = offset;
        TileBase = tileBase;
        TileSize = tileSize;
    }

    public Vector3 Position;

    public Color4b Color;

    public Vector2 TexCoord;

    public Vector2 Offset;

    public Vector2 TileBase;

    public Vector2 TileSize;

    /// <inheritdoc />
    public int AttribDescriptionCount => 6;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new(AttributeType.FloatVec3);

        // Color: vec4 (4 unsigned bytes, normalized)
        descriptions[1] = new(AttributeType.FloatVec4, true, AttributeBaseType.UnsignedByte);

        // TexCoord: vec2 (2 floats)
        descriptions[2] = new(AttributeType.FloatVec2);

        // Offset: vec2 (2 floats)
        descriptions[3] = new(AttributeType.FloatVec2);

        // TileBase: vec2 (2 floats)
        descriptions[4] = new(AttributeType.FloatVec2);

        // TileSize: vec2 (2 floats)
        descriptions[5] = new(AttributeType.FloatVec2);
    }
}
