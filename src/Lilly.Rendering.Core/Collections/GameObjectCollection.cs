using System.Collections;
using System.Runtime.CompilerServices;
using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Rendering.Core.Collections;

public class GameObjectCollection<TGameObject> : ICollection<TGameObject> where TGameObject : class, IGameObject
{
    private readonly List<TGameObject> _items;
    private int _version;

    public GameObjectCollection()
        => _items = [];

    public GameObjectCollection(int capacity)
        => _items = new(capacity);

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _items.Count;
    }

    public bool IsReadOnly => false;

    public TGameObject this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _items[index];
    }

    public struct Enumerator : IEnumerator<TGameObject>
    {
        private readonly GameObjectCollection<TGameObject> _collection;
        private readonly int _version;
        private int _index;
        private TGameObject? _current;

        internal Enumerator(GameObjectCollection<TGameObject> collection)
        {
            _collection = collection;
            _version = collection._version;
            _index = 0;
            _current = default;
        }

        public readonly TGameObject Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_version != _collection._version)
            {
                ThrowVersionMismatch();
            }

            if (_index < _collection._items.Count)
            {
                _current = _collection._items[_index];
                _index++;

                return true;
            }

            _current = default;

            return false;
        }

        public void Reset()
        {
            if (_version != _collection._version)
            {
                ThrowVersionMismatch();
            }

            _index = 0;
            _current = default;
        }

        private static void ThrowVersionMismatch()
        {
            throw new InvalidOperationException("Collection was modified during enumeration.");
        }
    }

    public void Add(TGameObject item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var index = FindInsertionIndex(item.ZIndex);
        _items.Insert(index, item);
        _version++;
    }

    public void AddRange(IEnumerable<TGameObject> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            Add(item);
        }
    }

    public void Clear()
    {
        _items.Clear();
        _version++;
    }

    public bool Contains(TGameObject item)
        => _items.Contains(item);

    public void CopyTo(TGameObject[] array, int arrayIndex)
    {
        _items.CopyTo(array, arrayIndex);
    }

    public IEnumerable<TGameObject> Flatten()
    {
        var stack = new Stack<IEnumerator<TGameObject>>();
        stack.Push(_items.GetEnumerator());

        try
        {
            while (stack.Count > 0)
            {
                var enumerator = stack.Peek();

                if (enumerator.MoveNext())
                {
                    var current = enumerator.Current;

                    yield return current;

                    var children = current.Children;

                    if (children.Any())
                    {
                        var typedChildren = children.OfType<TGameObject>();
                        stack.Push(typedChildren.GetEnumerator());
                    }
                }
                else
                {
                    enumerator.Dispose();
                    stack.Pop();
                }
            }
        }
        finally
        {
            while (stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }

    public IEnumerable<IGameObject> FlattenAll()
    {
        var stack = new Stack<IEnumerator<IGameObject>>();
        stack.Push(_items.Cast<IGameObject>().GetEnumerator());

        try
        {
            while (stack.Count > 0)
            {
                var enumerator = stack.Peek();

                if (enumerator.MoveNext())
                {
                    var current = enumerator.Current;

                    yield return current;

                    var children = current.Children;

                    if (children.Any())
                    {
                        stack.Push(children.GetEnumerator());
                    }
                }
                else
                {
                    enumerator.Dispose();
                    stack.Pop();
                }
            }
        }
        finally
        {
            while (stack.Count > 0)
            {
                stack.Pop().Dispose();
            }
        }
    }

    public Enumerator GetEnumerator()
        => new(this);

    public void NotifyZIndexChanged(TGameObject item)
    {
        if (_items.Remove(item))
        {
            var index = FindInsertionIndex(item.ZIndex);
            _items.Insert(index, item);
            _version++;
        }
    }

    public bool Remove(TGameObject item)
    {
        var removed = _items.Remove(item);

        if (removed)
        {
            _version++;
        }

        return removed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindInsertionIndex(uint zIndex)
    {
        var low = 0;
        var high = _items.Count - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var midZIndex = _items[mid].ZIndex;

            if (midZIndex <= zIndex)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    IEnumerator<TGameObject> IEnumerable<TGameObject>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
