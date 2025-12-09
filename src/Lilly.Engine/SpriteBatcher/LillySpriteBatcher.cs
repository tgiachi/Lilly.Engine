using System.Drawing;
using System.Numerics;
using Lilly.Engine.Extensions;
using Lilly.Engine.Fonts;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using TrippyGL;

namespace Lilly.Engine.SpriteBatcher;

/// <summary>
/// Provides high-level sprite batching functionality for drawing textures, text, and shapes.
/// </summary>
public class LillySpriteBatcher : ILillySpriteBatcher
{
    private readonly IAssetManager _assetManager;

    private readonly TextureBatcher _spriteBatcher;

    private readonly FontStashRenderer _fontRenderer;

    private readonly IDpiManager _dpiManager;

    /// <summary>
    /// Initializes a new instance of the LillySpriteBatcher class.
    /// </summary>
    /// <param name="assetManager">The asset manager for loading textures and fonts.</param>
    /// <param name="spriteBatcher">The underlying texture batcher.</param>
    /// <param name="fontRenderer">The font renderer for text drawing.</param>
    /// <param name="dpiManager">The DPI manager for scaling.</param>
    public LillySpriteBatcher(
        IAssetManager assetManager,
        TextureBatcher spriteBatcher,
        FontStashRenderer fontRenderer,
        IDpiManager dpiManager
    )
    {
        _assetManager = assetManager;
        _spriteBatcher = spriteBatcher;
        _fontRenderer = fontRenderer;
        _dpiManager = dpiManager;
    }

    /// <summary>
    /// Draws a hollow rectangle (border only).
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The color of the border.</param>
    /// <param name="thickness">The thickness of the border.</param>
    /// <param name="depth">The depth for layering.</param>
    public void DrawHollowRectangle(
        Vector2 position,
        Vector2 size,
        Color4b color,
        float thickness = 2.0f,
        float depth = 0.0f
    )
    {
        var whiteTexture = _assetManager.GetTexture<Texture2D>(DefaultTextures.WhiteTextureKey);

        if (whiteTexture == null)
        {
            throw new InvalidOperationException($"White texture '{DefaultTextures.WhiteTextureKey}' not found.");
        }

        var scaledPosition = position * _dpiManager.DPIScale;
        var scaledSize = size * _dpiManager.DPIScale;
        var scaledThickness = thickness * _dpiManager.DPIScale;

        // Top border
        _spriteBatcher.Draw(
            whiteTexture,
            scaledPosition,
            null,
            color,
            new Vector2(scaledSize.X, scaledThickness),
            0f,
            Vector2.Zero,
            depth
        );

        // Bottom border
        _spriteBatcher.Draw(
            whiteTexture,
            new(scaledPosition.X, scaledPosition.Y + scaledSize.Y - scaledThickness),
            null,
            color,
            new Vector2(scaledSize.X, scaledThickness),
            0f,
            Vector2.Zero,
            depth
        );

        // Left border
        _spriteBatcher.Draw(
            whiteTexture,
            scaledPosition,
            null,
            color,
            new Vector2(scaledThickness, scaledSize.Y),
            0f,
            Vector2.Zero,
            depth
        );

        // Right border
        _spriteBatcher.Draw(
            whiteTexture,
            new(scaledPosition.X + scaledSize.X - scaledThickness, scaledPosition.Y),
            null,
            color,
            new Vector2(scaledThickness, scaledSize.Y),
            0f,
            Vector2.Zero,
            depth
        );
    }

    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    /// <param name="position">The position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    /// <param name="color">The color of the rectangle.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="origin">The origin point for rotation.</param>
    /// <param name="depth">The depth for layering.</param>
    public void DrawRectangle(
        Vector2 position,
        Vector2 size,
        Color4b color,
        float rotation = 0f,
        Vector2? origin = null,
        float depth = 0.0f
    )
    {
        var whiteTexture = _assetManager.GetTexture<Texture2D>(DefaultTextures.WhiteTextureKey);

        if (whiteTexture == null)
        {
            throw new InvalidOperationException($"White texture '{DefaultTextures.WhiteTextureKey}' not found.");
        }

        var scaledPosition = position * _dpiManager.DPIScale;
        var scaledSize = size * _dpiManager.DPIScale;
        var scaledOrigin = (origin ?? Vector2.Zero) * _dpiManager.DPIScale;

        _spriteBatcher.Draw(
            whiteTexture,
            scaledPosition,
            null,
            color,
            scaledSize,
            rotation,
            scaledOrigin,
            depth
        );
    }

    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="size">The font size.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="position">The position to draw the text.</param>
    /// <param name="color">The color of the text.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="scale">The scale factor.</param>
    public void DrawText(
        string fontName,
        int size,
        string text,
        Vector2 position,
        Color4b color,
        float rotation = 0f,
        Vector2? scale = null
    )
    {
        var font = _assetManager.GetFont(fontName, size);

        if (font == null)
        {
            throw new InvalidOperationException($"Font '{fontName}' with size {size} not found.");
        }

        font.DrawText(
            _fontRenderer,
            text,
            position,
            color.ToFsColor(),
            rotation,
            scale: scale * _dpiManager.DPIScale ?? Vector2.One * _dpiManager.DPIScale
        );
    }

    /// <summary>
    /// Draws a texture at the specified position or destination rectangle.
    /// </summary>
    /// <param name="texture">The name of the texture.</param>
    /// <param name="position">The position to draw the texture (ignored if destination is set).</param>
    /// <param name="destination">The destination rectangle.</param>
    /// <param name="source">The source rectangle.</param>
    /// <param name="color">The color tint.</param>
    /// <param name="origin">The origin point for rotation and scaling.</param>
    /// <param name="scale">The scale factor.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="depth">The depth for layering.</param>
    public void DrawTexure(
        string texture,
        Vector2? position = null,
        Rectangle? destination = null,
        Rectangle? source = null,
        Color4b? color = null,
        Vector2? origin = null,
        Vector2? scale = null,
        float? rotation = null,
        float depth = 0.0f
    )
    {
        // Parse atlas@index syntax
        var actualTextureName = texture;
        Rectangle? atlasSource = null;

        if (!string.IsNullOrEmpty(texture) && texture.Contains('@'))
        {
            var parts = texture.Split('@', 2);
            var atlasName = parts[0];

            // Validate and parse tile index
            if (!int.TryParse(parts[1], out var tileIndex))
            {
                throw new ArgumentException($"Invalid tile index in '{texture}'. Expected format: 'atlasName@tileIndex'");
            }

            // Get atlas region (throws if atlas not found or index out of range)
            var region = _assetManager.GetAtlasRegion(atlasName, tileIndex);

            // Get underlying atlas texture name
            actualTextureName = atlasName + "_atlas";

            // Get texture to convert UV to pixels
            var atlasTex = _assetManager.GetTexture<Texture2D>(actualTextureName);

            // Convert UV coordinates to pixel coordinates
            var x = (int)(region.Position.X * atlasTex.Width);
            var y = (int)(region.Position.Y * atlasTex.Height);
            var width = (int)(region.Size.X * atlasTex.Width);
            var height = (int)(region.Size.Y * atlasTex.Height);

            // Create source rectangle for this tile (only if user hasn't specified one)
            atlasSource = new Rectangle(x, y, width, height);
        }

        // Use atlas source if available and user hasn't specified their own
        var finalSource = source ?? atlasSource;

        var tex = _assetManager.GetTexture<Texture2D>(actualTextureName);

        if (tex == null)
        {
            throw new InvalidOperationException($"Texture '{actualTextureName}' not found.");
        }

        var finalColor = color ?? Color4b.White;

        // If there's a destination rectangle, use that
        if (destination.HasValue)
        {
            var dest = destination.Value;
            var finalPosition = new Vector2(dest.X, dest.Y);
            var finalScale = new Vector2(dest.Width, dest.Height);

            // If there's a source rectangle, calculate scale based on the ratio
            if (finalSource.HasValue)
            {
                var src = finalSource.Value;
                finalScale = new(dest.Width / (float)src.Width, dest.Height / (float)src.Height);
            }
            else
            {
                // Use the texture dimensions
                finalScale = new(dest.Width / (float)tex.Width, dest.Height / (float)tex.Height);
            }

            // Apply DPI scaling
            finalPosition *= _dpiManager.DPIScale;
            finalScale *= _dpiManager.DPIScale;

            _spriteBatcher.Draw(
                tex,
                finalPosition,
                finalSource,
                finalColor,
                finalScale,
                0f,
                Vector2.Zero,
                depth
            );
        }

        // Otherwise use position + rotation + scale + origin
        else
        {
            var finalPosition = position ?? Vector2.Zero;
            var finalOrigin = origin ?? Vector2.Zero;
            var finalScale = scale ?? Vector2.One;
            var finalRotation = rotation ?? 0f;

            // Apply DPI scaling
            finalPosition *= _dpiManager.DPIScale;
            finalScale *= _dpiManager.DPIScale;

            _spriteBatcher.Draw(
                tex,
                finalPosition,
                finalSource,
                finalColor,
                finalScale,
                finalRotation,
                finalOrigin,
                depth
            );
        }
    }
}
