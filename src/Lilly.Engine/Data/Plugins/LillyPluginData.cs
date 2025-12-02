namespace Lilly.Engine.Data.Plugins;

/// <summary>
/// Represents metadata for an engine plugin including its identifier, name, version, author, and dependencies.
/// </summary>
public record LillyPluginData(string Id, string Name, string Version, string Author, params string[] Dependencies);
