using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// A non-interactive UI progress bar that displays a progress value visually.
/// Progress bars cannot receive focus and are read-only.
/// </summary>
public class ProgressBarGameObject : BaseGameObject2D
{
    private readonly IAssetManager _assetManager;
    private float _progress; // 0.0 to 1.0
    private int _width = 300;
    private int _height = 30;

    private const int BorderThickness = 2;

    /// <summary>
    /// Gets or sets the progress value (0.0 to 1.0).
    /// </summary>
    public float Progress
    {
        get => _progress;
        set => _progress = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Gets or sets the progress bar width.
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                UpdateTransformSize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the progress bar height.
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                UpdateTransformSize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the UI theme used for styling.
    /// </summary>
    public UITheme Theme { get; }

    /// <summary>
    /// Gets or sets the color of the filled progress bar.
    /// </summary>
    public Color4b BarColor { get; set; } = new(50, 200, 50); // LimeGreen equivalent

    /// <summary>
    /// Gets or sets the color of the background (unfilled portion).
    /// </summary>
    public Color4b BackgroundColor { get; set; } = Color4b.DarkGray;

    /// <summary>
    /// Gets or sets the color of the border.
    /// </summary>
    public Color4b BorderColor { get; set; } = Color4b.Black;

    /// <summary>
    /// Gets or sets the color of the percentage text.
    /// </summary>
    public Color4b TextColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets whether to display the progress percentage as text.
    /// </summary>
    public bool ShowPercentText { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to draw a border around the progress bar.
    /// </summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the ProgressBarGameObject class.
    /// </summary>
    /// <param name="assetManager">The asset manager service for loading fonts and textures.</param>
    /// <param name="theme">The theme to apply.</param>
    public ProgressBarGameObject(IAssetManager assetManager, UITheme theme)
    {
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));

        UpdateTransformSize();
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        var bounds = new Rectangle<float>(Transform.Position, new Vector2D<float>(_width, _height));

        // Draw background
        yield return DrawRectangle(bounds, BackgroundColor, depth: NextDepth());

        // Draw filled portion
        var filledWidth = _width * _progress;
        var filledBounds = new Rectangle<float>(
            Transform.Position,
            new Vector2D<float>(filledWidth, _height)
        );
        yield return DrawRectangle(filledBounds, BarColor, depth: NextDepth());

        // Draw border
        if (ShowBorder)
        {
            foreach (var cmd in DrawHollowRectangle(
                Transform.Position,
                new Vector2D<float>(_width, _height),
                BorderColor,
                BorderThickness,
                depth: NextDepth()))
            {
                yield return cmd;
            }
        }

        // Draw percentage text
        if (ShowPercentText)
        {
            var percentText = $"{_progress * 100:F0}%";
            var textPos = new Vector2D<float>(
                Transform.Position.X + _width / 2f - (percentText.Length * Theme.FontSize) / 4f,
                Transform.Position.Y + (_height - Theme.FontSize) / 2f
            );

            yield return DrawTextCustom(
                Theme.FontName,
                percentText,
                Theme.FontSize,
                textPos,
                color: TextColor,
                depth: NextDepth()
            );
        }
    }

    private void UpdateTransformSize()
    {
        Transform.Size = new Vector2D<float>(_width, _height);
    }
}
