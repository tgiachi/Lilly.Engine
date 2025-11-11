using Lilly.Engine.Scenes.Transitions.Base;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Squid.Engine.Scenes.Transitions;

/// <summary>
/// A transition effect that fades in a colored overlay.
/// </summary>
public class FadeTransition : Transition
{
    private readonly SpriteBatch? _spriteBatch;
    private SpriteBatch? _sharedSpriteBatch;

    /// <summary>
    /// Initializes a new instance of the FadeTransition class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    /// <param name="color">The color of the fading overlay.</param>
    /// <param name="duration">The duration of the transition in seconds.</param>
    public FadeTransition(GraphicsDevice graphicsDevice, Color color, float duration = 1.0f)
        : base(duration)
    {
        Color = color;
        GraphicsDevice = graphicsDevice;
        _spriteBatch = new(graphicsDevice);
    }

    /// <summary>
    /// Gets the color of the fading overlay.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Gets or sets the graphics device used for rendering.
    /// </summary>
    public GraphicsDevice? GraphicsDevice { get; set; }

    /// <summary>
    /// Disposes the transition resources.
    /// </summary>
    public override void Dispose()
    {
        _spriteBatch?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Renders the transition effect.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public override void Render(GameTime gameTime)
    {
        if (GraphicsDevice == null)
        {
            return;
        }

        // Use shared sprite batch if available, otherwise use own
        var spriteBatchToUse = _sharedSpriteBatch ?? _spriteBatch;

        if (spriteBatchToUse == null)
        {
            return;
        }

        spriteBatchToUse.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
        spriteBatchToUse.FillRectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, Color * Value);
        spriteBatchToUse.End();
    }

    /// <summary>
    /// Sets the rendering context for the transition.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="spriteBatch">The sprite batch to use.</param>
    public void SetRenderingContext(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        GraphicsDevice = graphicsDevice;
        _sharedSpriteBatch = spriteBatch;
    }
}
