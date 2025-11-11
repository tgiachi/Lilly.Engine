using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Utils;
using Lilly.Engine.Scenes.Transitions.Base;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Scenes.Transitions;

/// <summary>
/// A transition effect that fades in a colored overlay.
/// </summary>
public class FadeTransition : Transition
{
    private readonly int _viewportWidth;
    private readonly int _viewportHeight;

    /// <summary>
    /// Initializes a new instance of the FadeTransition class.
    /// </summary>
    /// <param name="viewportWidth">The viewport width for rendering.</param>
    /// <param name="viewportHeight">The viewport height for rendering.</param>
    /// <param name="color">The color of the fading overlay.</param>
    /// <param name="duration">The duration of the transition in seconds.</param>
    public FadeTransition(int viewportWidth, int viewportHeight, Color4b color, float duration = 1.0f)
        : base(duration)
    {
        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;
        Color = color;
    }

    /// <summary>
    /// Gets the color of the fading overlay.
    /// </summary>
    public Color4b Color { get; }

    /// <summary>
    /// Disposes the transition resources.
    /// </summary>
    public override void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Renders the transition effect using render commands.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <param name="renderPipeline">The render pipeline to enqueue commands to.</param>
    public override void Render(GameTime gameTime, IGraphicRenderPipeline renderPipeline)
    {
        // Apply alpha based on transition value
        var alpha = (byte)(Value * 255);
        var colorWithAlpha = new Color4b(Color.R, Color.G, Color.B, alpha);

        var destination = new Rectangle<float>(0, 0, _viewportWidth, _viewportHeight);

        var command = RenderCommandHelpers.CreateDrawTexture(
            new DrawTexturePayload(
                texture: DefaultTextures.WhiteTextureKey,
                destination: destination,
                color: colorWithAlpha,
                depth: 0.9f // High depth to render on top of most elements
            )
        );

        renderPipeline.EnqueueRenderCommand(command);
    }
}
