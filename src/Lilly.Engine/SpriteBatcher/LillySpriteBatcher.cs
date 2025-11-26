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

public class LillySpriteBatcher : ILillySpriteBatcher
{
    private readonly IAssetManager _assetManager;

    private readonly TextureBatcher _spriteBatcher;

    private readonly FontStashRenderer _fontRenderer;

    private readonly IDpiManager _dpiManager;

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
            rotation: rotation,
            scale: scale * _dpiManager.DPIScale ?? Vector2.One * _dpiManager.DPIScale
        );
    }

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
        var tex = _assetManager.GetTexture<Texture2D>(texture);

        if (tex == null)
        {
            throw new InvalidOperationException($"Texture '{texture}' not found.");
        }

        var finalColor = color ?? Color4b.White;

        // If there's a destination rectangle, use that
        if (destination.HasValue)
        {
            var dest = destination.Value;
            var finalPosition = new Vector2(dest.X, dest.Y);
            var finalScale = new Vector2(dest.Width, dest.Height);

            // If there's a source rectangle, calculate scale based on the ratio
            if (source.HasValue)
            {
                var src = source.Value;
                finalScale = new Vector2(dest.Width / (float)src.Width, dest.Height / (float)src.Height);
            }
            else
            {
                // Use the texture dimensions
                finalScale = new Vector2(dest.Width / (float)tex.Width, dest.Height / (float)tex.Height);
            }

            // Apply DPI scaling
            finalPosition *= _dpiManager.DPIScale;
            finalScale *= _dpiManager.DPIScale;

            _spriteBatcher.Draw(
                tex,
                finalPosition,
                source,
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
                source,
                finalColor,
                finalScale,
                finalRotation,
                finalOrigin,
                depth
            );
        }
    }

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
            new Vector2(scaledPosition.X, scaledPosition.Y + scaledSize.Y - scaledThickness),
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
            new Vector2(scaledPosition.X + scaledSize.X - scaledThickness, scaledPosition.Y),
            null,
            color,
            new Vector2(scaledThickness, scaledSize.Y),
            0f,
            Vector2.Zero,
            depth
        );
    }
}
