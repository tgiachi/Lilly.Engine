namespace Lilly.Engine.Core.Data.Privimitives;

/// <summary>
/// Represents game timing information for frame updates and rendering.
/// </summary>
public class GameTime
{
    /// <summary>
    /// Gets or sets the total elapsed game time since the start of the game.
    /// </summary>
    public double TotalGameTime { get; set; }

    /// <summary>
    /// Gets or sets the time elapsed since the last update.
    /// </summary>
    public double ElapsedGameTime { get; set; }

    /// <summary>
    /// Gets the elapsed game time as a TimeSpan.
    /// </summary>
    public TimeSpan ElapsedGameTimeAsTimeSpan => TimeSpan.FromMilliseconds(ElapsedGameTime);

    /// <summary>
    /// Gets the total game time as a TimeSpan.
    /// </summary>
    public TimeSpan TotalGameTimeAsTimeSpan => TimeSpan.FromMilliseconds(TotalGameTime);

    /// <summary>
    /// Returns a string representation of the game time.
    /// </summary>
    /// <returns>A string containing total and elapsed game time information.</returns>
    public override string ToString()
        => $"TotalGameTime: {TotalGameTime}s, ElapsedGameTime: {ElapsedGameTime}s";

    /// <summary>
    /// Updates the game time with the elapsed time since the last frame.
    /// </summary>
    /// <param name="elapsedMilliseconds">The time elapsed since the last frame in milliseconds.</param>
    public void Update(double elapsedMilliseconds)
    {
        ElapsedGameTime = elapsedMilliseconds;
        TotalGameTime += elapsedMilliseconds;
    }
}
