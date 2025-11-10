using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Payloads;
using Lilly.Engine.Rendering.Core.Primitives;
using Lilly.Engine.Rendering.Core.Utils;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Base.GameObjects;

/// <summary>
/// Base class for 2D game objects.
/// </summary>
public abstract class BaseGameObject2D : IGameObject2D
{
    /// <summary>
    /// Gets or sets the parent game object.
    /// </summary>
    public IGameObject? Parent { get; set; }

    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public GameObjectCollection<IGameObject> Children { get; } = new();

    /// <summary>
    /// Gets or sets the unique identifier for the game object.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the game object.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rendering order of the game object.
    /// </summary>
    public ushort Order { get; set; }

    /// <summary>
    /// Gets the 2D transform for position, rotation, and scale.
    /// </summary>
    public Transform2D Transform { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the game object is visible and should be rendered.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the layer of the game object.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Draws the game object by returning render commands.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <returns>An enumerable collection of render commands for this object.</returns>
    /// <summary>
    /// Draws the game object by returning render commands.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <returns>An enumerable collection of render commands for this object.</returns>
    protected abstract IEnumerable<RenderCommand> Draw(GameTime gameTime);

    /// <summary>
    /// Renders the game object and its children by collecting all render commands.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <returns>An enumerable collection of render commands from this object and all its children.</returns>
    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        // Yield commands from this object
        foreach (var command in Draw(gameTime))
        {
            yield return command;
        }

        // Yield commands from all children
        foreach (var child in Children)
        {
            foreach (var command in child.Render(gameTime))
            {
                yield return command;
            }
        }
    }

    /// <summary>
    /// Creates a render command to draw text using the object's Transform2D and Order.
    /// </summary>
    /// <param name="fontFamily">The font family to use.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="color">The text color.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <param name="useTransform">Whether to use the object's Transform2D (default: true).</param>
    /// <returns>A render command for drawing the text.</returns>
    protected RenderCommand DrawText(
        string fontFamily,
        string text,
        int fontSize,
        Color4b? color = null,
        float? depth = null,
        bool useTransform = true
    )
    {
        return RenderCommandHelpers.CreateDrawText(
            new DrawTextPayload(
                fontFamily,
                text,
                fontSize,
                useTransform ? Transform.Position : Vector2D<float>.Zero,
                useTransform ? Transform.Scale : null,
                useTransform ? Transform.Rotation : 0f,
                color,
                depth ?? Order
            )
        );
    }

    /// <summary>
    /// Creates a render command to draw text with custom transform parameters.
    /// </summary>
    /// <param name="fontFamily">The font family to use.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="fontSize">The font size.</param>
    /// <param name="position">The position to draw the text at.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="color">The text color.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <returns>A render command for drawing the text.</returns>
    protected RenderCommand DrawTextCustom(
        string fontFamily,
        string text,
        int fontSize,
        Vector2D<float> position,
        Vector2D<float>? scale = null,
        float rotation = 0f,
        Color4b? color = null,
        float? depth = null
    )
    {
        return RenderCommandHelpers.CreateDrawText(
            new DrawTextPayload(fontFamily, text, fontSize, position, scale, rotation, color, depth ?? Order)
        );
    }

    /// <summary>
    /// Creates a render command to draw a texture using the object's Transform2D and Order.
    /// </summary>
    /// <param name="texture">The name of the texture to draw.</param>
    /// <param name="source">The source rectangle within the texture.</param>
    /// <param name="color">The color tint to apply.</param>
    /// <param name="origin">The origin point for rotation and scaling.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <param name="useTransform">Whether to use the object's Transform2D (default: true).</param>
    /// <returns>A render command for drawing the texture.</returns>
    protected RenderCommand DrawTexture(
        string texture,
        Rectangle<float>? source = null,
        Color4b? color = null,
        Vector2D<float>? origin = null,
        float? depth = null,
        bool useTransform = true
    )
    {
        return RenderCommandHelpers.CreateDrawTexture(
            new DrawTexturePayload(
                texture,
                transform: null,
                position: useTransform ? Transform.Position : null,
                destination: null,
                source: source,
                color: color,
                origin: origin,
                scale: useTransform ? Transform.Scale : null,
                rotation: useTransform ? Transform.Rotation : null,
                depth: depth ?? Order
            )
        );
    }

    /// <summary>
    /// Creates a render command to draw a texture with custom transform parameters.
    /// </summary>
    /// <param name="texture">The name of the texture to draw.</param>
    /// <param name="transform">The transformation matrix to apply.</param>
    /// <param name="position">The position to draw the texture at.</param>
    /// <param name="destination">The destination rectangle.</param>
    /// <param name="source">The source rectangle within the texture.</param>
    /// <param name="color">The color tint to apply.</param>
    /// <param name="origin">The origin point for rotation and scaling.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <returns>A render command for drawing the texture.</returns>
    protected RenderCommand DrawTextureCustom(
        string texture,
        Matrix3X2<float>? transform = null,
        Vector2D<float>? position = null,
        Rectangle<float>? destination = null,
        Rectangle<float>? source = null,
        Color4b? color = null,
        Vector2D<float>? origin = null,
        Vector2D<float>? scale = null,
        float? rotation = null,
        float? depth = null
    )
    {
        return RenderCommandHelpers.CreateDrawTexture(
            new DrawTexturePayload(
                texture,
                transform,
                position,
                destination,
                source,
                color,
                origin,
                scale,
                rotation,
                depth ?? Order
            )
        );
    }

    /// <summary>
    /// Draws a filled rectangle using a white pixel texture.
    /// </summary>
    /// <param name="bounds">The rectangle bounds.</param>
    /// <param name="color">The color to apply.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <param name="whitePixelTexture">The name of the white pixel texture (defaults to DefaultTextures.WhiteTextureKey).</param>
    /// <returns>A render command for drawing the rectangle.</returns>
    protected RenderCommand DrawRectangle(
        Rectangle<float> bounds,
        Color4b color,
        float? depth = null,
        string? whitePixelTexture = null
    )
    {
        return RenderCommandHelpers.CreateDrawTexture(
            new DrawTexturePayload(
                whitePixelTexture ?? DefaultTextures.WhiteTextureKey,
                transform: null,
                position: null,
                destination: bounds,
                source: null,
                color: color,
                origin: null,
                scale: null,
                rotation: null,
                depth: depth ?? Order
            )
        );
    }

    /// <summary>
    /// Draws a border around a rectangle using a white pixel texture.
    /// </summary>
    /// <param name="bounds">The rectangle bounds.</param>
    /// <param name="color">The border color.</param>
    /// <param name="thickness">The border thickness.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <param name="whitePixelTexture">The name of the white pixel texture (defaults to DefaultTextures.WhiteTextureKey).</param>
    /// <returns>An enumerable of render commands for drawing the border (4 sides).</returns>
    protected IEnumerable<RenderCommand> DrawBorder(
        Rectangle<float> bounds,
        Color4b color,
        float thickness = 1.0f,
        float? depth = null,
        string? whitePixelTexture = null
    )
    {
        var textureKey = whitePixelTexture ?? DefaultTextures.WhiteTextureKey;

        // Top
        yield return DrawRectangle(
            new Rectangle<float>(bounds.Origin.X, bounds.Origin.Y, bounds.Size.X, thickness),
            color,
            depth,
            textureKey
        );

        // Bottom
        yield return DrawRectangle(
            new Rectangle<float>(
                bounds.Origin.X,
                bounds.Origin.Y + bounds.Size.Y - thickness,
                bounds.Size.X,
                thickness
            ),
            color,
            depth,
            textureKey
        );

        // Left
        yield return DrawRectangle(
            new Rectangle<float>(bounds.Origin.X, bounds.Origin.Y, thickness, bounds.Size.Y),
            color,
            depth,
            textureKey
        );

        // Right
        yield return DrawRectangle(
            new Rectangle<float>(
                bounds.Origin.X + bounds.Size.X - thickness,
                bounds.Origin.Y,
                thickness,
                bounds.Size.Y
            ),
            color,
            depth,
            textureKey
        );
    }

    /// <summary>
    /// Draws a hollow rectangle (outline only) using a white pixel texture.
    /// </summary>
    /// <param name="position">The top-left position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The border color.</param>
    /// <param name="thickness">The border thickness.</param>
    /// <param name="depth">The depth value for layering (defaults to Order if null).</param>
    /// <param name="whitePixelTexture">The name of the white pixel texture (defaults to DefaultTextures.WhiteTextureKey).</param>
    /// <returns>An enumerable of render commands for drawing the hollow rectangle.</returns>
    protected IEnumerable<RenderCommand> DrawHollowRectangle(
        Vector2D<float> position,
        Vector2D<float> size,
        Color4b color,
        float thickness = 1.0f,
        float? depth = null,
        string? whitePixelTexture = null
    )
    {
        var bounds = new Rectangle<float>(position, size);
        return DrawBorder(bounds, color, thickness, depth, whitePixelTexture);
    }
}
