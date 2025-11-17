namespace Lilly.Engine.Attributes;

/// <summary>
///  Specifies a custom name for a vertex property in rendering contexts.
/// </summary>
/// <param name="name"></param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class VertexPropertyNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
