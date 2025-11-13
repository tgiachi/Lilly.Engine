using FontStashSharp;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Utils;

/// <summary>
/// Provides text measurement utilities for UI components.
/// Uses FontStashSharp for accurate text measurements.
/// </summary>
public static class TextMeasurement
{
    /// <summary>
    /// Measures the size of a string when rendered with the specified font.
    /// </summary>
    /// <param name="assetManager">The asset manager to retrieve fonts from.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <returns>The size of the rendered text.</returns>
    public static Vector2D<float> MeasureString(IAssetManager assetManager, string text, string fontFamily, int fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Vector2D<float>.Zero;
        }

        var font = assetManager.GetFont<DynamicSpriteFont>(fontFamily, fontSize);
        var size = font.MeasureString(text);

        return new Vector2D<float>(size.X, size.Y);
    }

    /// <summary>
    /// Measures the width of a string when rendered with the specified font.
    /// </summary>
    /// <param name="assetManager">The asset manager to retrieve fonts from.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <returns>The width of the rendered text.</returns>
    public static float MeasureStringWidth(IAssetManager assetManager, string text, string fontFamily, int fontSize)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0f;
        }

        var font = assetManager.GetFont<DynamicSpriteFont>(fontFamily, fontSize);
        return font.MeasureString(text).X;
    }

    /// <summary>
    /// Finds the character index at the specified X position within the text.
    /// Used for click-to-position cursor functionality.
    /// </summary>
    /// <param name="assetManager">The asset manager to retrieve fonts from.</param>
    /// <param name="text">The text to search within.</param>
    /// <param name="xPosition">The X position to find the character at.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <returns>The character index at the specified position.</returns>
    public static int GetCharacterIndexAtPosition(IAssetManager assetManager, string text, float xPosition, string fontFamily, int fontSize)
    {
        if (string.IsNullOrEmpty(text) || xPosition <= 0)
        {
            return 0;
        }

        var font = assetManager.GetFont<DynamicSpriteFont>(fontFamily, fontSize);

        // Binary search would be faster, but linear search is simpler and text is usually short
        var currentX = 0f;

        for (var i = 0; i < text.Length; i++)
        {
            var charWidth = font.MeasureString(text[i].ToString()).X;
            var charMidpoint = currentX + charWidth / 2f;

            if (xPosition < charMidpoint)
            {
                return i;
            }

            currentX += charWidth;
        }

        return text.Length;
    }

    /// <summary>
    /// Gets the X position of the character at the specified index.
    /// Used for cursor positioning and selection rendering.
    /// </summary>
    /// <param name="assetManager">The asset manager to retrieve fonts from.</param>
    /// <param name="text">The text containing the character.</param>
    /// <param name="index">The character index.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <returns>The X position of the character at the specified index.</returns>
    public static float GetPositionAtCharacterIndex(IAssetManager assetManager, string text, int index, string fontFamily, int fontSize)
    {
        if (string.IsNullOrEmpty(text) || index <= 0)
        {
            return 0f;
        }

        if (index > text.Length)
        {
            index = text.Length;
        }

        var textBeforeIndex = text[..index];
        var font = assetManager.GetFont<DynamicSpriteFont>(fontFamily, fontSize);

        return font.MeasureString(textBeforeIndex).X;
    }

    /// <summary>
    /// Gets the height of the font at the specified size.
    /// </summary>
    /// <param name="assetManager">The asset manager to retrieve fonts from.</param>
    /// <param name="fontFamily">The font family name.</param>
    /// <param name="fontSize">The font size.</param>
    /// <returns>The height of the font.</returns>
    public static float GetFontHeight(IAssetManager assetManager, string fontFamily, int fontSize)
    {
        var font = assetManager.GetFont<DynamicSpriteFont>(fontFamily, fontSize);
        return font.LineHeight;
    }
}
