namespace Lilly.Engine.Core.Data.Privimitives;

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
    ///  Gets the elapsed game time as a TimeSpan.
    /// </summary>
    public TimeSpan ElapsedGameTimeAsTimeSpan => TimeSpan.FromMilliseconds(ElapsedGameTime);

    /// <summary>
    ///  Gets the total game time as a TimeSpan.
    /// </summary>
    public TimeSpan TotalGameTimeAsTimeSpan => TimeSpan.FromMilliseconds(TotalGameTime);

    public void Update(double elapsedMilliseconds)
    {
        ElapsedGameTime = elapsedMilliseconds;
        TotalGameTime += elapsedMilliseconds;
    }

    public override string ToString()
        => $"TotalGameTime: {TotalGameTime}s, ElapsedGameTime: {ElapsedGameTime}s";
}
