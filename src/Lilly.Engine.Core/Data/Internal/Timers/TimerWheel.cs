namespace Lilly.Engine.Core.Data.Internal.Timers;

/// <summary>
/// High-performance timer wheel data structure for managing timers with O(1) insertion, removal, and tick advancement.
/// </summary>
public class TimerWheel
{
    private readonly TimerWheelSlot[] _slots;
    private readonly int _numSlots;
    private readonly double _tickDurationMs;
    private int _currentSlot;
    private double _accumulatedTime;

    /// <summary>
    /// Gets the current slot index
    /// </summary>
    public int CurrentSlot => _currentSlot;

    /// <summary>
    /// Gets the total number of slots in the wheel
    /// </summary>
    public int SlotCount => _numSlots;

    /// <summary>
    /// Gets the duration of each tick in milliseconds
    /// </summary>
    public double TickDurationMs => _tickDurationMs;

    /// <summary>
    /// Initializes a new instance of the TimerWheel class
    /// </summary>
    /// <param name="numSlots">Number of slots in the wheel (should be power of 2 for efficiency, default 512)</param>
    /// <param name="tickDurationMs">Duration of each tick in milliseconds (default 16ms for ~60fps)</param>
    public TimerWheel(int numSlots = 512, double tickDurationMs = 16.0)
    {
        if (numSlots <= 0 || (numSlots & (numSlots - 1)) != 0)
        {
            throw new ArgumentException("Number of slots must be a power of 2", nameof(numSlots));
        }

        if (tickDurationMs <= 0)
        {
            throw new ArgumentException("Tick duration must be positive", nameof(tickDurationMs));
        }

        _numSlots = numSlots;
        _tickDurationMs = tickDurationMs;
        _currentSlot = 0;
        _accumulatedTime = 0;

        _slots = new TimerWheelSlot[_numSlots];
        for (int i = 0; i < _numSlots; i++)
        {
            _slots[i] = new TimerWheelSlot();
        }
    }

    /// <summary>
    /// Adds a timer to the wheel
    /// </summary>
    /// <param name="timer">The timer to add</param>
    /// <param name="delayMs">Delay in milliseconds before timer fires</param>
    public void AddTimer(TimerDataObject timer, double delayMs)
    {
        // Calculate total ticks until expiration
        int totalTicks = (int)Math.Ceiling(delayMs / _tickDurationMs);

        if (totalTicks <= 0)
        {
            totalTicks = 1; // Fire on next tick
        }

        // Calculate slot index and number of rounds
        int rounds = totalTicks / _numSlots;
        int ticksIntoWheel = totalTicks % _numSlots;

        int slotIndex = (_currentSlot + ticksIntoWheel) & (_numSlots - 1); // Fast modulo for power of 2

        timer.RemainingRounds = rounds;
        timer.SlotIndex = slotIndex;
        timer.SlotNode = _slots[slotIndex].Add(timer);
    }

    /// <summary>
    /// Removes a timer from the wheel
    /// </summary>
    /// <param name="timer">The timer to remove</param>
    /// <returns>True if timer was removed, false if not found</returns>
    public bool RemoveTimer(TimerDataObject timer)
    {
        if (timer.SlotNode == null || timer.SlotIndex < 0 || timer.SlotIndex >= _numSlots)
        {
            return false;
        }

        _slots[timer.SlotIndex].Remove(timer.SlotNode);
        timer.SlotNode = null;
        timer.SlotIndex = -1;

        return true;
    }

    /// <summary>
    /// Advances the wheel by the specified elapsed time and returns timers that have expired
    /// </summary>
    /// <param name="elapsedMs">Elapsed time in milliseconds</param>
    /// <returns>List of expired timers</returns>
    public List<TimerDataObject> Tick(double elapsedMs)
    {
        _accumulatedTime += elapsedMs;

        var expiredTimers = new List<TimerDataObject>();

        // Process complete ticks
        while (_accumulatedTime >= _tickDurationMs)
        {
            _accumulatedTime -= _tickDurationMs;
            _currentSlot = (_currentSlot + 1) & (_numSlots - 1); // Fast modulo for power of 2

            // Process timers in current slot
            var slot = _slots[_currentSlot];
            var timersInSlot = slot.DrainTimers();

            foreach (var timer in timersInSlot)
            {
                // Clear node reference since it's been removed from the slot
                timer.SlotNode = null;
                timer.SlotIndex = -1;

                if (timer.RemainingRounds > 0)
                {
                    // Timer needs more rounds, re-add it to the same slot
                    timer.RemainingRounds--;
                    timer.SlotIndex = _currentSlot;
                    timer.SlotNode = slot.Add(timer);
                }
                else
                {
                    // Timer has expired
                    expiredTimers.Add(timer);
                }
            }
        }

        return expiredTimers;
    }

    /// <summary>
    /// Gets the total number of timers currently in the wheel
    /// </summary>
    /// <returns>Total timer count</returns>
    public int GetTotalTimerCount()
    {
        int count = 0;
        foreach (var slot in _slots)
        {
            count += slot.Count;
        }
        return count;
    }

    /// <summary>
    /// Clears all timers from the wheel
    /// </summary>
    public void Clear()
    {
        foreach (var slot in _slots)
        {
            slot.Clear();
        }
        _currentSlot = 0;
        _accumulatedTime = 0;
    }
}
