namespace Lilly.Engine.Rendering.Core.Payloads.Shaders;

public readonly struct ShaderUniform
{
    public string Name { get; init; }
    public object Value { get; init; }
    public ShaderUniformType Type { get; init; }

    public int Unit { get; init; } = -1;

    public ShaderUniform(string name, ShaderUniformType type, object value)
    {
        Name = name;
        Type = type;
        Value = value;
    }

    public override string ToString()
        => $"ShaderUniform(Name: {Name}, Type: {Type}, Value: {Value})";
}
