using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

public class TextGameObject : BaseGameObject2D
{
    public string Text { get; set; } = "Debug Text";
    public Color4b Color { get; set; } = Color4b.Black;
    public string FontFamily { get; set; } = "defaultUiFont";
    public int FontSize { get; set; } = 32;

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return DrawText(FontFamily, Text, FontSize, Color);
    }
}
