namespace Lilly.Engine.Core.Data.Internal.Timers;

/// <summary>
/// High-performance timer wheel data structure for managing timers with O(1) insertion, removal, and tick advancement.
/// </summary>
public class TimerWheel
{
    private readonly TimerWheelSlot[] _slots;
    private double _accumulatedTime;

    /// <summary>
    /// Gets the current slot index
    /// </summary>
    public int CurrentSlot { get; private set; }

    /// <summary>
    /// Gets the total number of slots in the wheel
    /// </summary>
    public int SlotCount { get; }

    /// <summary>
    /// Gets the duration of each tick in milliseconds
    /// </summary>
    public double TickDurationMs { get; }

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

        SlotCount = numSlots;
        TickDurationMs = tickDurationMs;
        CurrentSlot = 0;
        _accumulatedTime = 0;

        _slots = new TimerWheelSlot[SlotCount];

        for (var i = 0; i < SlotCount; i++)
        {
            _slots[i] = new();
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
        var totalTicks = (int)Math.Ceiling(delayMs / TickDurationMs);

        if (totalTicks <= 0)
        {
            totalTicks = 1; // Fire on next tick
        }

        // Calculate slot index and number of rounds
        var rounds = totalTicks / SlotCount;
        var ticksIntoWheel = totalTicks % SlotCount;

        var slotIndex = (CurrentSlot + ticksIntoWheel) & (SlotCount - 1); // Fast modulo for power of 2

        timer.RemainingRounds = rounds;
        timer.SlotIndex = slotIndex;
        timer.SlotNode = _slots[slotIndex].Add(timer);
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
        CurrentSlot = 0;
        _accumulatedTime = 0;
    }

    /// <summary>
    /// Gets the total number of timers currently in the wheel
    /// </summary>
    /// <returns>Total timer count</returns>
    public int GetTotalTimerCount()
    {
        var count = 0;

        foreach (var slot in _slots)
        {
            count += slot.Count;
        }

        return count;
    }

    /// <summary>
    /// Removes a timer from the wheel
    /// </summary>
    /// <param name="timer">The timer to remove</param>
    /// <returns>True if timer was removed, false if not found</returns>
    public bool RemoveTimer(TimerDataObject timer)
    {
        if (timer.SlotNode == null || timer.SlotIndex < 0 || timer.SlotIndex >= SlotCount)
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
        while (_accumulatedTime >= TickDurationMs)
        {
            _accumulatedTime -= TickDurationMs;
            CurrentSlot = (CurrentSlot + 1) & (SlotCount - 1); // Fast modulo for power of 2

            // Process timers in current slot
            var slot = _slots[CurrentSlot];
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
                    timer.SlotIndex = CurrentSlot;
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
}
