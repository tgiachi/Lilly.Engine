using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using TrippyGL;

namespace Lilly.Engine.GameObjects;

public class TextGameObject : Base2dGameObject
{
    public string Text { get; set; }
    public int FontSize { get; set; } = 16;

    public Color4b Color { get; set; } = Color4b.White;

    public string FontName { get; set; } = "default";

    public Action<GameTime>? OnUpdateAction { get; set; }

    public TextGameObject() : base("TextGameObject") { }

    protected override void OnDraw(GameTime gameTime)
    {
        SpriteBatcher.DrawText(FontName, FontSize, Text, Transform.Position, Color, Transform.Rotation, Transform.Scale);
    }

    public override void Update(GameTime gameTime)
    {
        OnUpdateAction?.Invoke(gameTime);
    }
}
