using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Exceptions;
using Lilly.Engine.Interfaces.Plugins;

namespace Lilly.Engine.Plugins;

/// <summary>
/// Central registry for managing plugin lifecycle and tracking loaded plugins.
/// </summary>
public class PluginRegistry
{
    private readonly List<ILillyPlugin> _loadedPlugins = [];
    private readonly List<LillyPluginData> _loadedPluginData = [];
    private readonly PluginDependencyValidator _dependencyValidator;

    private List<string> _scriptEngineLoadFunctions = [];

    public PluginRegistry(PluginDependencyValidator dependencyValidator = null)
        => _dependencyValidator = dependencyValidator ?? new PluginDependencyValidator();

    /// <summary>
    /// Checks if a plugin can be loaded based on dependency requirements.
    /// </summary>
    public bool CanLoad(LillyPluginData pluginData)
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
    /// Checks for circular dependencies in a set of plugins.
    /// </summary>
    /// <returns>Empty list if no cycles detected, otherwise returns the cycle</returns>
    public List<string> CheckForCircularDependencies(IReadOnlyList<LillyPluginData> allPlugins)
        => _dependencyValidator.DetectCircularDependencies(allPlugins);

    /// <summary>
    /// Gets the dependency chain for a specific plugin.
    /// </summary>
    public IReadOnlyList<ILillyPlugin> GetDependencyChain(string pluginId)
    {
        var plugin = GetPluginById(pluginId);
        var chain = new List<ILillyPlugin> { plugin };

        if (plugin.LillyData.Dependencies != null)
        {
            foreach (var depId in plugin.LillyData.Dependencies)
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
    public IReadOnlyList<ILillyPlugin> GetDependentsOf(string pluginId)
    {
        return _loadedPlugins
               .Where(p => p.LillyData.Dependencies?.Contains(pluginId) ?? false)
               .ToList()
               .AsReadOnly();
    }

    /// <summary>
    /// Gets metadata for all loaded plugins.
    /// </summary>
    public IReadOnlyList<LillyPluginData> GetLoadedPluginData()
        => _loadedPluginData.AsReadOnly();

    /// <summary>
    /// Gets all currently loaded plugins.
    /// </summary>
    public IReadOnlyList<ILillyPlugin> GetLoadedPlugins()
        => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Gets a specific plugin by ID.
    /// </summary>
    public ILillyPlugin GetPluginById(string pluginId)
    {
        var plugin = _loadedPlugins.FirstOrDefault(p => p.LillyData.Id == pluginId);

        return plugin ?? throw new InvalidOperationException($"Plugin '{pluginId}' not found.");
    }

    /// <summary>
    /// Sorts plugins in dependency order (topological sort).
    /// </summary>
    public IEnumerable<LillyPluginData> GetPluginsInDependencyOrder(IReadOnlyList<LillyPluginData> allPlugins)
        => _dependencyValidator.TopologicalSort(allPlugins);

    /// <summary>
    /// Registers a plugin after validating its dependencies.
    /// </summary>
    /// <exception cref="PluginLoadException">If dependencies are missing or circular</exception>
    public void RegisterPlugin(ILillyPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var pluginData = plugin.LillyData;

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

        if (plugin.GetScriptOnLoadFunctionName() != null)
        {
            _scriptEngineLoadFunctions.Add(plugin.GetScriptOnLoadFunctionName());
        }
    }

    public IReadOnlyList<string> GetScriptEngineLoadFunctions()
        => _scriptEngineLoadFunctions.AsReadOnly();
}
