using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Utils;
using TrippyGL;

namespace Lilly.Engine.GameObjects;

public class TextGameObject : Base2dGameObject
{
    private string _text = string.Empty;
    private int _fontSize = 16;
    private string _fontName = "default";


    private readonly IAssetManager? _assetManager;

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                UpdateTextSize();
            }
        }
    }

    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                UpdateTextSize();
            }
        }
    }

    public Color4b Color { get; set; } = Color4b.White;

    public string FontName
    {
        get => _fontName;
        set
        {
            if (_fontName != value)
            {
                _fontName = value;
                UpdateTextSize();
            }
        }
    }

    public Action<GameTime>? OnUpdateAction { get; set; }

    public TextGameObject(IAssetManager? assetManager) : base("TextGameObject")
    {
        _assetManager = assetManager;
    }

    protected override void OnDraw(GameTime gameTime)
    {
        var worldPosition = GetWorldPosition();
        var worldRotation = GetWorldRotation();
        var worldScale = GetWorldScale();

        SpriteBatcher.DrawText(FontName, FontSize, Text, worldPosition, Color, worldRotation, worldScale);
    }

    public override void Update(GameTime gameTime)
    {
        OnUpdateAction?.Invoke(gameTime);
    }

    /// <summary>
    /// Updates the Transform.Size based on the current text, font, and font size.
    /// Only works if AssetManager is set.
    /// </summary>
    private void UpdateTextSize()
    {
        if (_assetManager == null || string.IsNullOrEmpty(_text))
        {
            return;
        }

        var size = TextMeasurement.MeasureString(_assetManager, _text, _fontName, _fontSize);
        Transform.Size = new(size.X, size.Y);
    }
}
