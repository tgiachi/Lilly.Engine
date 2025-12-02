using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Vertex layout used for rain drop streaks in the rain effect.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RainVertex : IVertex
{
    public RainVertex(
        Vector3 position,
        Vector2 corner,
        float length,
        float alpha)
    {
        Position = position;
        Corner = corner;
        Length = length;
        Alpha = alpha;
    }

    public Vector3 Position;
    public Vector2 Corner;
    public float Length;
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

        // Length: float (1 float) - Length of rain drop
        descriptions[2] = new VertexAttribDescription(AttributeType.Float);

        // Alpha: float (1 float) - Drop transparency
        descriptions[3] = new VertexAttribDescription(AttributeType.Float);
    }
}