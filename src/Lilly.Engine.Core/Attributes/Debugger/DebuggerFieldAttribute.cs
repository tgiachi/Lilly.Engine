namespace Lilly.Engine.Core.Attributes.Debugger;

/// <summary>
/// Marks a field or property for debugger display.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

/// <summary>
/// Attribute that marks a field or property for inclusion in debugger display.
/// </summary>
public class DebuggerFieldAttribute : Attribute { }
