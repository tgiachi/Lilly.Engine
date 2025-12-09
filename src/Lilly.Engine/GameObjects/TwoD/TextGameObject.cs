using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.TwoD;

/// <summary>
/// A game object that renders text using a specified font and size.
/// </summary>
public class TextGameObject : Base2dGameObject
{
    private string _text = string.Empty;
    private int _fontSize = 16;
    private string _fontName = "default";

    private readonly IAssetManager? _assetManager;

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the font size for the text.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color4b Color { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the name of the font to use.
    /// </summary>
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

    /// <summary>
    /// Gets or sets an optional action to invoke during update.
    /// </summary>
    public Action<GameTime>? OnUpdateAction { get; set; }

    /// <summary>
    /// Initializes a new instance of the TextGameObject class.
    /// </summary>
    /// <param name="assetManager">The asset manager for loading fonts.</param>
    public TextGameObject(IAssetManager? assetManager, IGameObjectManager gameObjectManager) : base(
        "TextGameObject",
        gameObjectManager
    )
        => _assetManager = assetManager;

    /// <summary>
    /// Updates the game object and invokes the optional update action.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    public override void Update(GameTime gameTime)
    {
        OnUpdateAction?.Invoke(gameTime);
    }

    /// <summary>
    /// Draws the text using the specified font and size.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    protected override void OnDraw(GameTime gameTime)
    {
        var worldPosition = GetWorldPosition();
        var worldRotation = GetWorldRotation();
        var worldScale = GetWorldScale();

        SpriteBatcher.DrawText(FontName, FontSize, Text, worldPosition, Color, worldRotation, worldScale);
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
