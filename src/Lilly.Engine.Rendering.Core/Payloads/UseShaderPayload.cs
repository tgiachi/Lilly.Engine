using Lilly.Engine.Rendering.Core.Payloads.Shaders;
using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Payloads;

public struct UseShaderPayload
{
    public uint ShaderHandle { get; init; } = 0;

    public Dictionary<string, ShaderUniform> Uniforms { get; init; } = new();

    public UseShaderPayload(uint shaderHandle)
    {
        ShaderHandle = shaderHandle;
    }

    public void SetUniform(string name, float value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Float,
            Value = value
        };
    }

    public void SetUniform(string name, int value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Int,
            Value = value
        };
    }

    public void SetUniform(string name, Vector2D<float> value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Vec2,
            Value = value
        };
    }

    public void SetUniform(string name, Vector3D<float> value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Vec3,
            Value = value
        };
    }

    public void SetUniform(string name, Vector4D<float> value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Vec4,
            Value = value
        };
    }

    public void SetUniform(string name, Matrix3X2<float> value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Mat3,
            Value = value
        };
    }

    public void SetUniform(string name, Matrix4X4<float> value)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Mat4,
            Value = value
        };
    }

    public void SetUniform(string name, uint textureHandle)
    {
        // last unit
        var unit = Uniforms.Values
                           .Where(u => u.Type == ShaderUniformType.Texture2D)
                           .Select(u => u.Unit)
                           .DefaultIfEmpty(-1)
                           .Max() +
                   1;
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Texture2D,
            Value = textureHandle,
            Unit = unit
        };
    }

    public void SetUniformSampler(string name, uint samplerHandle)
    {
        Uniforms[name] = new ShaderUniform
        {
            Name = name,
            Type = ShaderUniformType.Sampler2D,
            Value = samplerHandle
        };
    }

    public override string ToString()
        => $"UseShaderPayload(ShaderHandle: {ShaderHandle}, Uniforms: [{string.Join(", ", Uniforms.Values)}])";
}
