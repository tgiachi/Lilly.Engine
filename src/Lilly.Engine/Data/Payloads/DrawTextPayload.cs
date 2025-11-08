using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.Data.Payloads;

public readonly struct DrawTextPayload
{
    public string FontFamily { get; init; }
    public string Text { get; init; }
    public float FontSize { get; init; }
    public Vector2D<float> Position { get; init; }
    public Vector2D<float> Scale { get; init; }
    public float Rotation { get; init; }
    public Color4b Color { get; init; }


    public DrawTextPayload(
        string fontFamily,
        string text,
        float fontSize,
        Vector2D<float> position,
        Vector2D<float>? scale = null,
        float rotation = 0f,
        Color4b? color = null
    )
    {
        color ??= Color4b.White;
        scale ??= new Vector2D<float>(1f, 1f);

        FontFamily = fontFamily;
        Text = text;
        FontSize = fontSize;
        Position = position;
        Scale = scale.Value;
        Rotation = rotation;
        Color = color.Value;

    }
}
