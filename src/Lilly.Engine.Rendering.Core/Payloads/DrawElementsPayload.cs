using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

public struct DrawElementsPayload
{
    public ShaderProgram ShaderProgram { get; init; }

    public VertexArray VertexArray { get; init; }

    public uint IndexCount { get; init; }

    public PrimitiveType PrimitiveType { get; init; }

    public uint StartIndex { get; init; }

    public DrawElementsPayload(
        ShaderProgram shaderProgram,
        VertexArray vertexArray,
        uint indexCount,
        PrimitiveType primitiveType = PrimitiveType.Triangles,
        uint startIndex = 0
    )
    {
        ShaderProgram = shaderProgram;
        VertexArray = vertexArray;
        IndexCount = indexCount;
        PrimitiveType = primitiveType;
        StartIndex = startIndex;
    }
}
