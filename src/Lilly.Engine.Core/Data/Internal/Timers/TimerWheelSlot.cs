namespace Lilly.Engine.Core.Data.Internal.Timers;

/// <summary>
/// Represents a single slot in the timer wheel containing timers that expire at this tick
/// </summary>
public class TimerWheelSlot
{
    private readonly LinkedList<TimerDataObject> _timers = new();

    /// <summary>
    /// Gets the number of timers in this slot
    /// </summary>
    public int Count => _timers.Count;

    /// <summary>
    /// Adds a timer to this slot and returns the node for O(1) removal
    /// </summary>
    /// <param name="timer">The timer to add</param>
    /// <returns>The linked list node for efficient removal</returns>
    public LinkedListNode<TimerDataObject> Add(TimerDataObject timer)
    {
        return _timers.AddLast(timer);
    }

    /// <summary>
    /// Removes a timer from this slot using its node reference
    /// </summary>
    /// <param name="node">The node to remove</param>
    public void Remove(LinkedListNode<TimerDataObject> node)
    {
        if (node.List == _timers)
        {
            _timers.Remove(node);
        }
    }

    /// <summary>
    /// Gets all timers in this slot and clears the slot
    /// </summary>
    /// <returns>List of timers that were in this slot</returns>
    public List<TimerDataObject> DrainTimers()
    {
        var result = new List<TimerDataObject>(_timers.Count);

        foreach (var timer in _timers)
        {
            result.Add(timer);
        }

        _timers.Clear();
        return result;
    }

    /// <summary>
    /// Gets an enumerator for iterating over timers without removing them
    /// </summary>
    /// <returns>Enumerator for the timers</returns>
    public LinkedList<TimerDataObject>.Enumerator GetEnumerator()
    {
        return _timers.GetEnumerator();
    }

    /// <summary>
    /// Clears all timers from this slot
    /// </summary>
    public void Clear()
    {
        _timers.Clear();
    }
}
