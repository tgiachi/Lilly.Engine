using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

[StructLayout(LayoutKind.Sequential)]
public readonly struct UseShaderPayload
{
    public ShaderProgram ShaderProgram { get; init; }

    public UseShaderPayload(ShaderProgram shaderProgram)
    {
        ShaderProgram = shaderProgram;
    }
}
