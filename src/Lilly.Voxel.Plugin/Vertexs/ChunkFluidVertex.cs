using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Vertex layout used for water and other fluid quads.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ChunkFluidVertex : IVertex
{
    public ChunkFluidVertex(
        Vector3 position,
        Color4b color,
        Vector2 texCoord,
        Vector2 tileBase,
        Vector2 tileSize,
        float direction,
        float top
    )
    {
        Position = position;
        Color = color;
        TexCoord = texCoord;
        TileBase = tileBase;
        TileSize = tileSize;
        Direction = direction;
        Top = top;
    }

    public Vector3 Position;
    public Color4b Color;
    public Vector2 TexCoord;
    public Vector2 TileBase;
    public Vector2 TileSize;
    public float Direction;
    public float Top;

    /// <inheritdoc />
    public int AttribDescriptionCount => 7;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new(AttributeType.FloatVec3);

        // Color: vec4 (4 unsigned bytes, normalized)
        descriptions[1] = new(AttributeType.FloatVec4, true, AttributeBaseType.UnsignedByte);

        // TexCoord: vec2 (2 floats)
        descriptions[2] = new(AttributeType.FloatVec2);

        // TileBase: vec2 (2 floats)
        descriptions[3] = new(AttributeType.FloatVec2);

        // TileSize: vec2 (2 floats)
        descriptions[4] = new(AttributeType.FloatVec2);

        // Direction: float (1 float)
        descriptions[5] = new(AttributeType.Float);

        // Top: float (1 float)
        descriptions[6] = new(AttributeType.Float);
    }
}
