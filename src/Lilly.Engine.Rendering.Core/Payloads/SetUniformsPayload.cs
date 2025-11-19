using System;
using System.Runtime.InteropServices;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload for applying shader uniforms as part of the render command stream.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct SetUniformsPayload
{
    public ShaderProgram ShaderProgram { get; init; }

    public Action<ShaderProgram>? ApplyUniforms { get; init; }

    public SetUniformsPayload(ShaderProgram shaderProgram, Action<ShaderProgram> applyUniforms)
    {
        ShaderProgram = shaderProgram ?? throw new ArgumentNullException(nameof(shaderProgram));
        ApplyUniforms = applyUniforms ?? throw new ArgumentNullException(nameof(applyUniforms));
    }

    /// <summary>
    /// Applies the configured uniforms on the shader program.
    /// </summary>
    public void Apply()
    {
        ApplyUniforms?.Invoke(ShaderProgram);
    }
}
