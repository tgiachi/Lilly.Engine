using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Represents a game object that renders text.
/// </summary>
public class TextGameObject : BaseGameObject2D
{
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

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return DrawText(FontFamily, Text, FontSize, Color);
    }
}
