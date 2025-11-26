using System.Drawing;
using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Scenes.Transitions.Base;
using Lilly.Engine.Scenes.Transitions.Interfaces;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using TrippyGL;

namespace Lilly.Engine.Scenes.Transitions;

/// <summary>
/// A transition effect that fades in a colored overlay.
/// </summary>
public class FadeTransition : TransitionGameObject
{
    private readonly FadeTransitionEffect _effect;

    /// <summary>
    /// Initializes a new instance of the FadeTransition class.
    /// </summary>
    /// <param name="viewportWidth">The viewport width for rendering.</param>
    /// <param name="viewportHeight">The viewport height for rendering.</param>
    /// <param name="color">The color of the fading overlay.</param>
    /// <param name="duration">The duration of the transition in seconds.</param>
    public FadeTransition(int viewportWidth, int viewportHeight, Color4b color, float duration = 1.0f)
        : base(duration, new FadeTransitionEffect(viewportWidth, viewportHeight, color))
    {
        _effect = (FadeTransitionEffect)Effect;
        Name = "FadeTransition";
    }

    /// <summary>
    /// Gets the color of the fading overlay.
    /// </summary>
    public Color4b Color => _effect.Color;

    /// <summary>
    /// Fade transition effect implementation.
    /// </summary>
    private sealed class FadeTransitionEffect : ITransitionEffect
    {
        private readonly int _viewportWidth;
        private readonly int _viewportHeight;

        public FadeTransitionEffect(int viewportWidth, int viewportHeight, Color4b color)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            Color = color;
        }

        public Color4b Color { get; }

        public void Render(GameTime gameTime, float progress, ILillySpriteBatcher spriteBatcher)
        {
            // Apply alpha based on transition progress
            var alpha = (byte)(progress * 255);
            var colorWithAlpha = new Color4b(Color.R, Color.G, Color.B, alpha);

            // Draw full-screen rectangle with fade color
            spriteBatcher.DrawRectangle(
                position: Vector2.Zero,
                size: new Vector2(_viewportWidth, _viewportHeight),
                color: colorWithAlpha,
                rotation: 0f,
                origin: null,
                depth: 0.9f
            );
        }
    }
}
