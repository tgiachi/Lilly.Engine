using Lilly.Engine.Data.Plugins;

namespace Lilly.Engine.Exceptions;

/// <summary>
/// Exception thrown when a plugin fails to load or validate.
/// </summary>
public class PluginLoadException : Exception
{
    public string PluginId { get; }
    public LillyPluginData PluginData { get; }
    public IReadOnlyList<LillyPluginData> LoadedPlugins { get; }

    public PluginLoadException(
        string message,
        string pluginId,
        LillyPluginData pluginData,
        IReadOnlyList<LillyPluginData> loadedPlugins,
        Exception innerException = null
    )
        : base(message, innerException)
    {
        PluginId = pluginId;
        PluginData = pluginData;
        LoadedPlugins = loadedPlugins;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        var loadedPluginsStr = string.Join(", ", LoadedPlugins.Select(p => $"{p.Id}@{p.Version}"));

        return $"{baseString}\n\nPlugin: {PluginId} v{PluginData.Version}\n" +
               $"Already Loaded: [{loadedPluginsStr}]";
    }
}
