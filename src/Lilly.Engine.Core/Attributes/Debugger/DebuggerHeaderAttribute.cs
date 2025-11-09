namespace Lilly.Engine.Core.Attributes.Debugger;

/// <summary>Specifies a header for the debugger display.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]

/// <summary>
/// Attribute used to specify a custom header for debugger display of classes or structs.
/// </summary>
public class DebuggerHeaderAttribute : Attribute
{
    /// <summary>Initializes a new instance of the DebuggerHeaderAttribute class.</summary>
    /// <param name="header">The header text.</param>
    public DebuggerHeaderAttribute(string header)
        => Header = header;

    /// <summary>Gets the header.</summary>
    public string Header { get; }
}
