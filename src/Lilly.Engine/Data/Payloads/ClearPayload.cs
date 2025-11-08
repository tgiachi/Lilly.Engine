using TrippyGL;

namespace Lilly.Engine.Data.Payloads;

public readonly struct ClearPayload
{
    public Color4b Color { get; init; }

    public ClearPayload(Color4b? color = null)
    {
        Color = color ?? Color4b.Black;
    }
}
