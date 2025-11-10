using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Debuggers;

public class TextGameObject : BaseGameObject2D
{
    public string Text { get; set; } = "Debug Text";
    public Color4b Color { get; set; } = Color4b.Black;
    public string FontFamily { get; set; } = "defaultUiFont";
    public int FontSize { get; set; } = 32;

    public Vector2D<float> Scale { get; set; } = new Vector2D<float>(1, 1);

    public Vector2D<float> Position { get; set; } = new Vector2D<float>(100, 100);

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        yield return DrawText(FontFamily, Text, FontSize, Position, Scale, 0f, Color);
    }
}
