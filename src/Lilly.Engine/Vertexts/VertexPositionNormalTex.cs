using System.Numerics;
using System.Runtime.InteropServices;
using Lilly.Engine.Attributes;
using TrippyGL;

namespace Lilly.Engine.Vertexts;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionNormalTex : IVertex
{
    [VertexPropertyName("Position")]
    public Vector3 Position;

    [VertexPropertyName("Normal")]
    public Vector3 Normal;

    [VertexPropertyName("TexCoords")]
    public Vector2 TexCoords;

    public VertexPositionNormalTex(Vector3 position, Vector3 normal, Vector2 texCoords)
    {
        Position = position;
        Normal = normal;
        TexCoords = texCoords;
    }

    public int AttribDescriptionCount => 3;

    public void WriteAttribDescriptions(Span<VertexAttribDescription> descriptions)
    {
        descriptions[0] = new VertexAttribDescription(AttributeType.FloatVec3);
        descriptions[1] = new VertexAttribDescription(AttributeType.FloatVec3);
        descriptions[2] = new VertexAttribDescription(AttributeType.FloatVec2);
    }
}
