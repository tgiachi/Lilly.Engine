namespace Lilly.Engine.Core.Data.Scripts.Container;

/// <summary>
/// Record containing data about a script module for internal processing.
/// </summary>
/// <param name="ModuleType">The .NET type of the script module.</param>
public record ScriptModuleData(Type ModuleType);
