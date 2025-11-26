using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;

namespace Lilly.Engine.GameObjects;

/// <summary>
/// A text game object that displays the current FPS (Frames Per Second).
/// </summary>
public class FpsGameObject : TextGameObject
{
    private double _fpsUpdateTimer;
    private int _frameCount;
    private double _fps;
    private readonly double _updateInterval;

    /// <summary>
    /// Gets or sets the format string for displaying FPS.
    /// Default is "FPS: {0:F1}".
    /// </summary>
    public string FpsFormat { get; set; } = "FPS: {0:F1}";



    /// <summary>
    /// Initializes a new instance of the <see cref="FpsGameObject"/> class with an AssetManager.
    /// </summary>
    /// <param name="assetManager">Optional AssetManager for automatic text size calculation.</param>
    /// <param name="updateInterval">The interval in seconds at which to update the FPS display. Default is 0.5 seconds.</param>
    public FpsGameObject(IAssetManager? assetManager, double updateInterval = 0.5) : base(assetManager)
    {
        _updateInterval = updateInterval;
        _fpsUpdateTimer = 0;
        _frameCount = 0;
        _fps = 0;
        Text = string.Format(FpsFormat, 0);
    }

    /// <summary>
    /// Updates the FPS calculation and display.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _frameCount++;
        _fpsUpdateTimer += gameTime.GetElapsedSeconds();

        // Update FPS display at the specified interval
        if (_fpsUpdateTimer >= _updateInterval)
        {
            _fps = _frameCount / _fpsUpdateTimer;
            Text = string.Format(FpsFormat, _fps);

            // Reset counters
            _frameCount = 0;
            _fpsUpdateTimer = 0;
        }
    }
}
