using Lilly.Engine.Scenes.Transitions.Base;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace Squid.Engine.Scenes.Transitions;

/// <summary>
/// A transition effect that expands a colored rectangle from the center.
/// </summary>
public class ExpandTransition : Transition
{
    private readonly SpriteBatch? _spriteBatch;
    private SpriteBatch? _sharedSpriteBatch;

    /// <summary>
    /// Initializes a new instance of the ExpandTransition class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device for rendering.</param>
    /// <param name="color">The color of the expanding rectangle.</param>
    /// <param name="duration">The duration of the transition in seconds.</param>
    public ExpandTransition(GraphicsDevice graphicsDevice, Color color, float duration = 1.0f)
        : base(duration)
    {
        Color = color;
        GraphicsDevice = graphicsDevice;
        _spriteBatch = new(graphicsDevice);
    }

    /// <summary>
    /// Gets the color of the expanding rectangle.
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

        var halfWidth = GraphicsDevice.Viewport.Width / 2f;
        var halfHeight = GraphicsDevice.Viewport.Height / 2f;
        var x = halfWidth * (1.0f - Value);
        var y = halfHeight * (1.0f - Value);
        var width = GraphicsDevice.Viewport.Width * Value;
        var height = GraphicsDevice.Viewport.Height * Value;
        var rectangle = new RectangleF(x, y, width, height);

        spriteBatchToUse.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp);
        spriteBatchToUse.FillRectangle(rectangle, Color);
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
