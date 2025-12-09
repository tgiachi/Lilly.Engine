using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Vertexs;

/// <summary>
/// Vertex layout for cloud face-based rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CloudsVertex : IVertex
{
    public CloudsVertex(Vector3 position, Vector3 normal)
    {
        Position = position;
        Normal = normal;
    }

    public Vector3 Position;
    public Vector3 Normal;

    /// <inheritdoc />
    public int AttribDescriptionCount => 2;

    /// <inheritdoc />
    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats)
        descriptions[0] = new(AttributeType.FloatVec3);

        // Normal: vec3 (3 floats)
        descriptions[1] = new(AttributeType.FloatVec3);
    }
}
