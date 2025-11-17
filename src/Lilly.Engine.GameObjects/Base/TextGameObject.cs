using FontStashSharp;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Represents a game object that renders text.
/// </summary>
public class TextGameObject : BaseGameObject2D
{
    private readonly IAssetManager? _assetManager;

    /// <summary>
    /// Gets or sets the text to be displayed.
    /// </summary>
    public string Text { get; set; } = "Debug Text";

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color4b Color { get; set; } = Color4b.Black;

    /// <summary>
    /// Gets or sets the font family used for the text.
    /// </summary>
    public string FontFamily { get; set; } = "defaultUiFont";

    /// <summary>
    /// Gets or sets the font size of the text.
    /// </summary>
    public int FontSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether the text should be centered on its position (default: false, top-left aligned).
    /// </summary>
    public bool CenterText { get; set; }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        Vector2D<float>? origin = null;

        if (CenterText && _assetManager != null)
        {
            var font = _assetManager.GetFont<DynamicSpriteFont>(FontFamily, FontSize);

            var size = font.MeasureString(Text);
            origin = new Vector2D<float>(size.X / 2f, size.Y / 2f);
        }

        yield return DrawText(FontFamily, Text, FontSize, Color, origin: origin);
    }
}
