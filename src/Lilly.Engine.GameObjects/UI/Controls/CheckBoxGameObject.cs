using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using TrippyGL;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// A focusable checkbox component that displays a clickable box with optional text label.
/// Supports both graphic rendering (checkbox square) and font-based rendering for checked/unchecked states.
/// </summary>
public class CheckBoxGameObject : BaseGameObject2D, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private bool _isChecked;
    private string _label = string.Empty;
    private bool _hasFocus;
    private bool _isMouseInBounds;

    private const int CheckBoxSize = 20;
    private const int BorderThickness = 2;
    private const int LabelPaddingX = 8;

    // Font-based symbols
    private string _checkedSymbol = "✓";
    private string _uncheckedSymbol = "☐";

    /// <summary>
    /// Gets or sets whether the checkbox is checked.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                OnCheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the label text displayed next to the checkbox.
    /// </summary>
    public string Label
    {
        get => _label;
        set => _label = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets whether to use font symbols for rendering (true) or graphic rendering (false).
    /// </summary>
    public bool UseFontSymbols { get; set; } = true;

    /// <summary>
    /// Gets or sets the symbol to display when the checkbox is checked (font-based rendering).
    /// </summary>
    public string CheckedSymbol
    {
        get => _checkedSymbol;
        set => _checkedSymbol = value ?? "✓";
    }

    /// <summary>
    /// Gets or sets the symbol to display when the checkbox is unchecked (font-based rendering).
    /// </summary>
    public string UncheckedSymbol
    {
        get => _uncheckedSymbol;
        set => _uncheckedSymbol = value ?? "☐";
    }

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color4b TextColor { get; set; } = Color4b.Black;

    /// <summary>
    /// Gets or sets the color of the checkbox background.
    /// </summary>
    public Color4b BackgroundColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the background color when focused.
    /// </summary>
    public Color4b BackgroundColorFocused { get; set; } = new(220, 230, 255);

    /// <summary>
    /// Gets or sets the background color when checked.
    /// </summary>
    public Color4b BackgroundColorChecked { get; set; } = new(100, 150, 255);

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color4b BorderColor { get; set; } = Color4b.DarkGray;

    /// <summary>
    /// Gets or sets the border color when focused.
    /// </summary>
    public Color4b BorderColorFocused { get; set; } = Color4b.Blue;

    /// <summary>
    /// Gets or sets the border color when checked.
    /// </summary>
    public Color4b BorderColorChecked { get; set; } = new(50, 100, 200);

    /// <summary>
    /// Gets or sets the check mark color (graphic rendering).
    /// </summary>
    public Color4b CheckMarkColor { get; set; } = Color4b.Green;

    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public UITheme Theme { get; }

    /// <summary>
    /// Event raised when the checked state changes.
    /// </summary>
    public event EventHandler? OnCheckedChanged;

    /// <summary>
    /// Initializes a new instance of CheckBoxGameObject.
    /// </summary>
    public CheckBoxGameObject(IInputManagerService inputManager, IAssetManager assetManager, UITheme theme)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));

        Transform.Size = new Vector2D<float>(200, 30);
    }

    public bool IsFocusable => true;

    public bool HasFocus
    {
        get => _hasFocus;
        set => _hasFocus = value;
    }

    public Rectangle<int> Bounds
        => new(
            new((int)Transform.Position.X, (int)Transform.Position.Y),
            new((int)Transform.Size.X, (int)Transform.Size.Y)
        );

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus)
        {
            return;
        }

        // Toggle on Space or Enter
        if ((IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Space) ||
             IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Enter)))
        {
            IsChecked = !IsChecked;
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        var wasInBounds = _isMouseInBounds;
        _isMouseInBounds = IsMouseInBounds(mousePos);

        var bounds = Bounds;
        var checkBoxRect = new Rectangle<int>(
            new(bounds.Origin.X, bounds.Origin.Y),
            new(CheckBoxSize, CheckBoxSize)
        );

        if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && RectContains(checkBoxRect, mousePos))
        {
            IsChecked = !IsChecked;
        }
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented for checkbox
    }

    public bool IsMouseInBounds(Vector2 mousePosition)
    {
        var bounds = Bounds;
        return bounds.Contains(new Vector2D<int>((int)mousePosition.X, (int)mousePosition.Y));
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        var bounds = Bounds;
        var checkBoxRect = new Rectangle<float>(
            Transform.Position,
            new Vector2D<float>(CheckBoxSize, CheckBoxSize)
        );

        // Determine colors
        var bgColor = _isChecked ? BackgroundColorChecked :
                      HasFocus ? BackgroundColorFocused :
                      BackgroundColor;
        var brColor = _isChecked ? BorderColorChecked :
                      HasFocus ? BorderColorFocused :
                      BorderColor;

        // Draw checkbox background
        yield return DrawRectangle(checkBoxRect, bgColor, depth: 0.50f);

        // Draw checkbox border
        foreach (var cmd in DrawHollowRectangle(
            Transform.Position,
            new Vector2D<float>(CheckBoxSize, CheckBoxSize),
            brColor,
            BorderThickness,
            depth: 0.51f))
        {
            yield return cmd;
        }

        // Draw checkmark or symbol
        if (UseFontSymbols)
        {
            // Font-based rendering with symbol
            var symbol = _isChecked ? _checkedSymbol : _uncheckedSymbol;
            var textPos = new Vector2D<float>(
                Transform.Position.X + 2,
                Transform.Position.Y + 2
            );

            yield return DrawTextCustom(
                Theme.FontName,
                symbol,
                Theme.FontSize,
                textPos,
                color: TextColor,
                depth: 0.52f
            );
        }
        else if (_isChecked)
        {
            // Graphic checkmark - simple diagonal lines
            // Draw a checkmark using rectangles (simplified)
            var centerX = Transform.Position.X + CheckBoxSize / 2;
            var centerY = Transform.Position.Y + CheckBoxSize / 2;

            yield return DrawRectangle(
                new Rectangle<float>(
                    new Vector2D<float>(centerX - 4, centerY - 2),
                    new Vector2D<float>(6, 4)
                ),
                CheckMarkColor,
                depth: 0.52f
            );
        }

        // Draw label
        if (!string.IsNullOrEmpty(_label))
        {
            var labelPos = new Vector2D<float>(
                Transform.Position.X + CheckBoxSize + LabelPaddingX,
                Transform.Position.Y + (CheckBoxSize - Theme.FontSize) / 2
            );

            yield return DrawTextCustom(
                Theme.FontName,
                _label,
                Theme.FontSize,
                labelPos,
                color: TextColor,
                depth: 0.52f
            );
        }
    }

    private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Key key)
    {
        return current.IsKeyPressed(key) && !previous.IsKeyPressed(key);
    }

    private static bool RectContains(Rectangle<int> rect, Vector2 point)
    {
        return rect.Contains(new Vector2D<int>((int)point.X, (int)point.Y));
    }
}
