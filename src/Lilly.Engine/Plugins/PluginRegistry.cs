using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Exceptions;
using Lilly.Engine.Interfaces.Plugins;

namespace Lilly.Engine.Plugins;

/// <summary>
/// Central registry for managing plugin lifecycle and tracking loaded plugins.
/// </summary>
public class PluginRegistry
{
    private readonly List<IEnginePlugin> _loadedPlugins = new();
    private readonly List<EnginePluginData> _loadedPluginData = new();
    private readonly PluginDependencyValidator _dependencyValidator;

    public PluginRegistry(PluginDependencyValidator dependencyValidator = null)
    {
        _dependencyValidator = dependencyValidator ?? new PluginDependencyValidator();
    }

    /// <summary>
    /// Gets all currently loaded plugins.
    /// </summary>
    public IReadOnlyList<IEnginePlugin> GetLoadedPlugins()
        => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Gets metadata for all loaded plugins.
    /// </summary>
    public IReadOnlyList<EnginePluginData> GetLoadedPluginData()
        => _loadedPluginData.AsReadOnly();

    /// <summary>
    /// Gets a specific plugin by ID.
    /// </summary>
    public IEnginePlugin GetPluginById(string pluginId)
    {
        var plugin = _loadedPlugins.FirstOrDefault(p => p.EngineData.Id == pluginId);

        return plugin ?? throw new InvalidOperationException($"Plugin '{pluginId}' not found.");
    }

    /// <summary>
    /// Checks if a plugin can be loaded based on dependency requirements.
    /// </summary>
    public bool CanLoad(EnginePluginData pluginData)
    {
        try
        {
            _dependencyValidator.ValidateDependencies(pluginData, GetLoadedPluginData());

            return true;
        }
        catch (PluginLoadException)
        {
            return false;
        }
    }

    /// <summary>
    /// Registers a plugin after validating its dependencies.
    /// </summary>
    /// <exception cref="PluginLoadException">If dependencies are missing or circular</exception>
    public void RegisterPlugin(IEnginePlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var pluginData = plugin.EngineData;

        // Check if already loaded
        if (_loadedPluginData.Any(p => p.Id == pluginData.Id))
        {
            throw new PluginLoadException(
                $"Plugin '{pluginData.Id}' is already loaded.",
                pluginData.Id,
                pluginData,
                GetLoadedPluginData()
            );
        }

        // Validate dependencies
        _dependencyValidator.ValidateDependencies(pluginData, GetLoadedPluginData());

        // Register the plugin
        _loadedPlugins.Add(plugin);
        _loadedPluginData.Add(pluginData);
    }

    /// <summary>
    /// Checks for circular dependencies in a set of plugins.
    /// </summary>
    /// <returns>Empty list if no cycles detected, otherwise returns the cycle</returns>
    public List<string> CheckForCircularDependencies(IReadOnlyList<EnginePluginData> allPlugins)
    {
        return _dependencyValidator.DetectCircularDependencies(allPlugins);
    }

    /// <summary>
    /// Sorts plugins in dependency order (topological sort).
    /// </summary>
    public IEnumerable<EnginePluginData> GetPluginsInDependencyOrder(IReadOnlyList<EnginePluginData> allPlugins)
    {
        return _dependencyValidator.TopologicalSort(allPlugins);
    }

    /// <summary>
    /// Gets the dependency chain for a specific plugin.
    /// </summary>
    public IReadOnlyList<IEnginePlugin> GetDependencyChain(string pluginId)
    {
        var plugin = GetPluginById(pluginId);
        var chain = new List<IEnginePlugin> { plugin };

        if (plugin.EngineData.Dependencies != null)
        {
            foreach (var depId in plugin.EngineData.Dependencies)
            {
                var dep = GetPluginById(depId);
                chain.AddRange(GetDependencyChain(depId));
            }
        }

        return chain.Distinct().ToList();
    }

    /// <summary>
    /// Gets plugins that depend on the specified plugin.
    /// </summary>
    public IReadOnlyList<IEnginePlugin> GetDependentsOf(string pluginId)
    {
        return _loadedPlugins
               .Where(p => p.EngineData.Dependencies?.Contains(pluginId) ?? false)
               .ToList()
               .AsReadOnly();
    }
}
