using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

public struct DrawArrayPayload
{
    public ShaderProgram ShaderProgram { get; init; }

    public VertexArray VertexArray { get; init; }

    public uint VertexCount { get; init; }

    public DrawArrayPayload(ShaderProgram shaderProgram, VertexArray vertexArray, uint vertexCount)
    {
        ShaderProgram = shaderProgram;
        VertexArray = vertexArray;
        VertexCount = vertexCount;
    }
}
