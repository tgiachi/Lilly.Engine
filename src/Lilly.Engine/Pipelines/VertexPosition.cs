using System.Numerics;
using System.Runtime.InteropServices;
using Lilly.Engine.Attributes;
using TrippyGL;

namespace Lilly.Engine.Pipelines;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPosition : IVertex
{
    [VertexPropertyName("Position")] public Vector3 Position;

    public VertexPosition(Vector3 position)
        => Position = position;

    public int AttribDescriptionCount => 1;

    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        // Use full qualification to avoid ambiguity between Silk.NET and TrippyGL
        descriptions[0] = new(AttributeType.FloatVec3);
    }
}
