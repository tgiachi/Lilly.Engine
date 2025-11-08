using System.Runtime.InteropServices;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Collections;

/// <summary>
/// High-performance collection for managing game objects.
/// Optimized for fast iteration with minimal allocations.
/// Automatically sorts objects by their Order property.
/// </summary>
/// <typeparam name="T">The type of game object, must implement IGameObject.</typeparam>
public sealed class GameObjectCollection<T> where T : IGameObject
{
    private readonly List<T> _gameObjects;
    private bool _isDirty;

    /// <summary>
    /// Gets the number of game objects in the collection.
    /// </summary>
    public int Count => _gameObjects.Count;

    /// <summary>
    /// Initializes a new instance of the GameObjectCollection class.
    /// </summary>
    public GameObjectCollection()
    {
        _gameObjects = new();
        _isDirty = false;
    }

    /// <summary>
    /// Initializes a new instance of the GameObjectCollection class with a specified capacity.
    /// </summary>
    /// <param name="capacity">Initial capacity for the collection.</param>
    public GameObjectCollection(int capacity)
    {
        _gameObjects = new(capacity);
        _isDirty = false;
    }

    /// <summary>
    /// Adds a game object to the collection.
    /// The collection will be sorted by Order on next access.
    /// </summary>
    /// <param name="gameObject">The game object to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
    public void Add(T gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        _gameObjects.Add(gameObject);
        _isDirty = true;
    }

    /// <summary>
    /// Adds a range of game objects to the collection.
    /// </summary>
    /// <param name="gameObjects">The game objects to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when gameObjects is null.</exception>
    public void AddRange(IEnumerable<T> gameObjects)
    {
        ArgumentNullException.ThrowIfNull(gameObjects);

        _gameObjects.AddRange(gameObjects);
        _isDirty = true;
    }

    /// <summary>
    /// Clears all game objects from the collection.
    /// </summary>
    public void Clear()
    {
        _gameObjects.Clear();
        _isDirty = false;
    }

    /// <summary>
    /// Checks if the collection contains a specific game object.
    /// </summary>
    /// <param name="gameObject">The game object to check.</param>
    /// <returns>True if the game object exists, false otherwise.</returns>
    public bool Contains(T gameObject)
        => _gameObjects.Contains(gameObject);

    /// <summary>
    /// Checks if the collection contains a game object with the specified ID.
    /// </summary>
    /// <param name="id">The ID to check.</param>
    /// <returns>True if a game object with the ID exists, false otherwise.</returns>
    public bool ContainsId(uint id)
        => _gameObjects.Exists(go => go.Id == id);

    /// <summary>
    /// Executes an action for each game object in Order.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    public void ForEach(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        EnsureSorted();

        foreach (var gameObject in _gameObjects)
        {
            action(gameObject);
        }
    }

    /// <summary>
    /// Gets a game object by its ID.
    /// </summary>
    /// <param name="id">The ID of the game object.</param>
    /// <returns>The game object if found, otherwise null.</returns>
    public T? GetById(uint id)
    {
        return _gameObjects.Find(go => go.Id == id);
    }

    /// <summary>
    /// Gets an enumerator for iterating over all game objects.
    /// Game objects are automatically sorted by Order before enumeration.
    /// </summary>
    /// <returns>An enumerator.</returns>
    public List<T>.Enumerator GetEnumerator()
    {
        EnsureSorted();
        return _gameObjects.GetEnumerator();
    }

    /// <summary>
    /// Gets a read-only span of all game objects sorted by Order.
    /// Use this for custom iteration scenarios.
    /// </summary>
    /// <returns>A read-only span of game objects.</returns>
    public ReadOnlySpan<T> GetSpan()
    {
        EnsureSorted();
        return CollectionsMarshal.AsSpan(_gameObjects);
    }

    /// <summary>
    /// Removes a specific game object from the collection.
    /// </summary>
    /// <param name="gameObject">The game object to remove.</param>
    /// <returns>True if the game object was removed, false if not found.</returns>
    public bool Remove(T gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        return _gameObjects.Remove(gameObject);
    }

    /// <summary>
    /// Removes a game object by its ID.
    /// </summary>
    /// <param name="id">The ID of the game object to remove.</param>
    /// <returns>True if the game object was removed, false if not found.</returns>
    public bool RemoveById(uint id)
    {
        var index = _gameObjects.FindIndex(go => go.Id == id);
        if (index >= 0)
        {
            _gameObjects.RemoveAt(index);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all game objects that match the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The number of game objects removed.</returns>
    public int RemoveAll(Predicate<T> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _gameObjects.RemoveAll(predicate);
    }

    /// <summary>
    /// Tries to get a game object by its ID.
    /// </summary>
    /// <param name="id">The ID of the game object.</param>
    /// <param name="gameObject">The game object if found, otherwise null.</param>
    /// <returns>True if the game object was found, false otherwise.</returns>
    public bool TryGetById(uint id, out T? gameObject)
    {
        gameObject = _gameObjects.Find(go => go.Id == id);
        return gameObject != null;
    }

    /// <summary>
    /// Ensures game objects are sorted by their Order property.
    /// Only sorts if the collection has been modified.
    /// </summary>
    private void EnsureSorted()
    {
        if (_isDirty)
        {
            _gameObjects.Sort((a, b) => a.Order.CompareTo(b.Order));
            _isDirty = false;
        }
    }
}
