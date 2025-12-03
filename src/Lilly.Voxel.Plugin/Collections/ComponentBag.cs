using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Collections;


/// <summary>
///  A collection that holds actionable components associated with a block or entity.
/// </summary>
public class ComponentBag
{
    private readonly Dictionary<Type, IActionableComponent> _map = new();

    public T? Get<T>() where T : class, IActionableComponent
        => _map.TryGetValue(typeof(T), out var c) ? (T)c : null;

    public void Set<T>(T component) where T : class, IActionableComponent
        => _map[typeof(T)] = component;

    public bool Remove<T>() where T : class, IActionableComponent
        => _map.Remove(typeof(T));

    public IEnumerable<IActionableComponent> All => _map.Values;

    public bool Has(Type componentType)
        => _map.ContainsKey(componentType);

    public void Merge(ComponentBag other, bool overwrite = false)
    {
        foreach (var kv in other._map)
        {
            if (overwrite || !_map.ContainsKey(kv.Key))
            {
                _map[kv.Key] = kv.Value;
            }
        }
    }
}
