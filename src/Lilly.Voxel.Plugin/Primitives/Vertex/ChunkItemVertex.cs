using System.Runtime.InteropServices;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Primitives.Vertex;

/// <summary>
/// Defines the vertex layout used by item billboard shaders.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ChunkItemVertex : IVertex
{
    public ChunkItemVertex(Vector3D<float> position, Vector4D<byte> color, Vector2D<float> texCoord, Vector2D<float> offset, Vector2D<float> tileBase, Vector2D<float> tileSize)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
        Offset = offset;
        TileBase = tileBase;
        TileSize = tileSize;
    }

    public Vector3D<float> Position;

    public Vector4D<byte> Color;

    public Vector2D<float> TexCoord;

    public Vector2D<float> Offset;

    public Vector2D<float> TileBase;

    public Vector2D<float> TileSize;

    /// <inheritdoc />
    public int AttribDescriptionCount => 6;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3);

        // Color: vec4 (4 unsigned bytes, normalized)
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec4, normalized: true, dataBaseType: AttributeBaseType.UnsignedByte);

        // TexCoord: vec2 (2 floats)
        descriptions[2] = new VertexAttribDescription(AttributeType.FloatVec2);

        // Offset: vec2 (2 floats)
        descriptions[3] = new VertexAttribDescription(AttributeType.FloatVec2);

        // TileBase: vec2 (2 floats)
        descriptions[4] = new VertexAttribDescription(AttributeType.FloatVec2);

        // TileSize: vec2 (2 floats)
        descriptions[5] = new VertexAttribDescription(AttributeType.FloatVec2);
    }
}
