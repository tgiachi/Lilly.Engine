using System.Runtime.InteropServices;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Vertex layout for cloud face-based rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CloudsVertex : IVertex
{
    public CloudsVertex(Vector3D<float> position, Vector3D<float> normal)
    {
        Position = position;
        Normal = normal;
    }

    public Vector3D<float> Position;
    public Vector3D<float> Normal;

    /// <inheritdoc />
    public int AttribDescriptionCount => 2;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3);

        // Normal: vec3 (3 floats)
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec3);
    }
}
