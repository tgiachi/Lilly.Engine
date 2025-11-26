using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Scenes.Transitions.Base;
using Lilly.Engine.Scenes.Transitions.Interfaces;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using TrippyGL;

namespace Lilly.Engine.Scenes.Transitions;

/// <summary>
/// A transition effect that expands a colored rectangle from the center.
/// </summary>
public class ExpandTransition : TransitionGameObject
{
    private readonly ExpandTransitionEffect _effect;

    /// <summary>
    /// Initializes a new instance of the ExpandTransition class.
    /// </summary>
    /// <param name="viewportWidth">The viewport width for rendering.</param>
    /// <param name="viewportHeight">The viewport height for rendering.</param>
    /// <param name="color">The color of the expanding rectangle.</param>
    /// <param name="duration">The duration of the transition in seconds.</param>
    public ExpandTransition(int viewportWidth, int viewportHeight, Color4b color, float duration = 1.0f)
        : base(duration, new ExpandTransitionEffect(viewportWidth, viewportHeight, color))
    {
        _effect = (ExpandTransitionEffect)Effect;
        Name = "ExpandTransition";
    }

    /// <summary>
    /// Gets the color of the expanding rectangle.
    /// </summary>
    public Color4b Color => _effect.Color;

    /// <summary>
    /// Expand transition effect implementation.
    /// </summary>
    private sealed class ExpandTransitionEffect : ITransitionEffect
    {
        private readonly int _viewportWidth;
        private readonly int _viewportHeight;

        public ExpandTransitionEffect(int viewportWidth, int viewportHeight, Color4b color)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            Color = color;
        }

        public Color4b Color { get; }

        public void Render(GameTime gameTime, float progress, ILillySpriteBatcher spriteBatcher)
        {
            // Calculate expanding rectangle from center
            var halfWidth = _viewportWidth / 2f;
            var halfHeight = _viewportHeight / 2f;
            var x = halfWidth * (1.0f - progress);
            var y = halfHeight * (1.0f - progress);
            var width = _viewportWidth * progress;
            var height = _viewportHeight * progress;

            // Draw expanding rectangle
            spriteBatcher.DrawRectangle(
                position: new Vector2(x, y),
                size: new Vector2(width, height),
                color: Color,
                rotation: 0f,
                origin: null,
                depth: 0.9f
            );
        }
    }
}
