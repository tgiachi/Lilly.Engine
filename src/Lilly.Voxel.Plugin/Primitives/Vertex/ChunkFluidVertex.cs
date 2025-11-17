using System.Runtime.InteropServices;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Primitives.Vertex;

/// <summary>
/// Vertex layout used for water and other fluid quads.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ChunkFluidVertex : IVertex
{
    public ChunkFluidVertex(
        Vector3D<float> position,
        Vector4D<byte> color,
        Vector2D<float> texCoord,
        Vector2D<float> tileBase,
        Vector2D<float> tileSize,
        float direction,
        float top)
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
        TileBase = tileBase;
        TileSize = tileSize;
        Direction = direction;
        Top = top;
    }

    public Vector3D<float> Position;
    public Vector4D<byte> Color;
    public Vector2D<float> TexCoord;
    public Vector2D<float> TileBase;
    public Vector2D<float> TileSize;
    public float Direction;
    public float Top;

    /// <inheritdoc />
    public int AttribDescriptionCount => 7;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3);

        // Color: vec4 (4 unsigned bytes, normalized)
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec4, normalized: true, dataBaseType: AttributeBaseType.UnsignedByte);

        // TexCoord: vec2 (2 floats)
        descriptions[2] = new VertexAttribDescription(AttributeType.FloatVec2);

        // TileBase: vec2 (2 floats)
        descriptions[3] = new VertexAttribDescription(AttributeType.FloatVec2);

        // TileSize: vec2 (2 floats)
        descriptions[4] = new VertexAttribDescription(AttributeType.FloatVec2);

        // Direction: float (1 float)
        descriptions[5] = new VertexAttribDescription(AttributeType.Float);

        // Top: float (1 float)
        descriptions[6] = new VertexAttribDescription(AttributeType.Float);
    }
}
