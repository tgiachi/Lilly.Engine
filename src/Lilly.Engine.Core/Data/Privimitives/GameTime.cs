namespace Lilly.Engine.Core.Data.Privimitives;

/// <summary>
/// Represents game timing information for frame updates and rendering.
/// </summary>
public readonly struct GameTime
{
    /// <summary>
    /// Gets the total elapsed game time since the start of the game in milliseconds.
    /// </summary>
    public double TotalGameTime { get; init; }

    /// <summary>
    /// Gets the time elapsed since the last update in milliseconds.
    /// </summary>
    public double ElapsedGameTime { get; init; }

    /// <summary>
    /// Gets the elapsed game time as a TimeSpan.
    /// </summary>
    public TimeSpan ElapsedGameTimeAsTimeSpan => TimeSpan.FromMilliseconds(ElapsedGameTime);

    /// <summary>
    /// Gets the total game time as a TimeSpan.
    /// </summary>
    public TimeSpan TotalGameTimeAsTimeSpan => TimeSpan.FromMilliseconds(TotalGameTime);


    public float GetTotalGameTimeSeconds() => (float)(TotalGameTime / 1000.0);

    public float GetElapsedSeconds() => (float)(ElapsedGameTime / 1000.0);

    /// <summary>
    /// Initializes a new instance of the GameTime struct.
    /// </summary>
    /// <param name="totalGameTime">The total elapsed game time in milliseconds.</param>
    /// <param name="elapsedGameTime">The time elapsed since the last update in milliseconds.</param>
    public GameTime(double totalGameTime, double elapsedGameTime)
    {
        TotalGameTime = totalGameTime;
        ElapsedGameTime = elapsedGameTime;
    }

    /// <summary>
    /// Creates a new GameTime with updated elapsed time.
    /// </summary>
    /// <param name="elapsedSeconds">The time elapsed since the last frame in seconds.</param>
    /// <returns>A new GameTime instance with updated values.</returns>
    public GameTime Update(double elapsedSeconds)
    {
        var elapsedMs = elapsedSeconds * 1000.0; // Convert to milliseconds
        return new GameTime(TotalGameTime + elapsedMs, elapsedMs);
    }

    /// <summary>
    /// Returns a string representation of the game time.
    /// </summary>
    /// <returns>A string containing total and elapsed game time information.</returns>
    public override string ToString()
        => $"TotalGameTime: {TotalGameTime}ms, ElapsedGameTime: {ElapsedGameTime}ms";
}
