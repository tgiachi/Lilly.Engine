using System.Drawing;
using System.Numerics;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.SpriteBatcher;

public interface ILillySpriteBatcher
{
    void DrawText(
        string fontName,
        int size,
        string text,
        Vector2 position,
        Color4b color,
        float rotation = 0f,
        Vector2? scale = null
    );

    void DrawTexure(
        string texture,
        Vector2? position = null,
        Rectangle? destination = null,
        Rectangle? source = null,
        Color4b? color = null,
        Vector2? origin = null,
        Vector2? scale = null,
        float? rotation = null,
        float depth = 0.0f
    );
}
