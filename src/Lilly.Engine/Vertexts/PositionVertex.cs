using System.Runtime.InteropServices;
using System.Numerics;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Engine.Vertexts;

[StructLayout(LayoutKind.Sequential)]
public struct PositionVertex : IVertex
{
    public Vector3 Position;

    public PositionVertex(Vector3 position)
    {
        Position = position;
    }

    public int AttribDescriptionCount => 1;

    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Position: vec3 (3 floats) -> location 0
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3);
    }
}
