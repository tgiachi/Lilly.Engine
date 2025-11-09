namespace Lilly.Engine.Core.Attributes.Scripts;

/// <summary>
/// Attribute to mark a method as a script function that will be exposed to JavaScript.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]

/// <summary>
/// Attribute that marks a method as a script function exposed to scripting languages.
/// </summary>
public class ScriptFunctionAttribute(string? functionName = null, string? helpText = null) : Attribute
{
    /// <summary>
    /// Gets the optional name override for the script function.
    /// </summary>
    public string? FunctionName { get; } = functionName;

    /// <summary>
    /// Gets the optional help text describing the function's purpose.
    /// </summary>
    public string? HelpText { get; } = helpText;
}
