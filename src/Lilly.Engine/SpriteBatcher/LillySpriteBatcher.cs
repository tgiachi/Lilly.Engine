using System.Numerics;
using Lilly.Engine.Extensions;
using Lilly.Engine.Fonts;
using Lilly.Engine.Interfaces.Services;
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

    public void Draw(
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
}
