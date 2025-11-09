namespace Lilly.Engine.Core.Attributes.Scripts;

/// <summary>
/// Attribute to mark a class as a script module that will be exposed to JavaScript.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]

/// <summary>
/// Attribute that marks a class as a script module exposed to scripting languages.
/// </summary>
public class ScriptModuleAttribute(string name, string? helpText = null) : Attribute
{
    /// <summary>Gets the name under which the module will be accessible in JavaScript.</summary>
    public string Name { get; } = name;

    /// <summary>Gets the optional help text describing the module's purpose.</summary>
    public string? HelpText { get; } = helpText;
}
