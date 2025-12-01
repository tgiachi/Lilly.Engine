using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Vertex layout used for snow particle quads in the snow effect.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SnowVertex : IVertex
{
    public SnowVertex(
        Vector3 position,
        Vector2 corner,
        float size,
        float alpha)
    {
        Position = position;
        Corner = corner;
        Size = size;
        Alpha = alpha;
    }

    public Vector3 Position;
    public Vector2 Corner;
    public float Size;
    public float Alpha;

    /// <inheritdoc />
    public int AttribDescriptionCount => 4;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3);

        // Corner: vec2 (2 floats) - Billboard corner coordinates (0-1)
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec2);

        // Size: float (1 float) - Particle size
        descriptions[2] = new VertexAttribDescription(AttributeType.Float);

        // Alpha: float (1 float) - Particle transparency
        descriptions[3] = new VertexAttribDescription(AttributeType.Float);
    }
}