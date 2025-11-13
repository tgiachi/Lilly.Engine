using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Utils;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using TrippyGL;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// Visual states for a button.
/// </summary>
public enum ButtonState
{
    Normal,
    Hover,
    Pressed,
    Disabled
}

/// <summary>
/// Size modes for button sizing.
/// </summary>
public enum ButtonSizeMode
{
    Fixed,
    Auto
}

/// <summary>
/// A clickable button control with support for text, icons, and visual states.
/// </summary>
public class ButtonGameObject : BaseGameObject2D, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private string _text = string.Empty;
    private bool _isEnabled = true;
    private ButtonState _currentState = ButtonState.Normal;
    private bool _isMouseDown;
    private bool _isMouseInBounds;
    private bool _hasFocus;
    private int _width = 100;
    private int _height = 32;
    private ButtonSizeMode _sizeMode = ButtonSizeMode.Fixed;

    private const int DefaultPadding = 8;
    private const int IconTextSpacing = 4;

    /// <summary>
    /// Initializes a new instance of the ButtonGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    /// <param name="assetManager">The asset manager for loading fonts and textures.</param>
    /// <param name="theme">The UI theme to use for styling.</param>
    public ButtonGameObject(IInputManagerService inputManager, IAssetManager assetManager, UITheme theme)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        UpdateTransformSize();
    }

    /// <summary>
    /// Gets the UI theme used for styling.
    /// </summary>
    public UITheme Theme { get; }

    /// <summary>
    /// Gets or sets the text displayed on the button.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;

                if (_sizeMode == ButtonSizeMode.Auto)
                {
                    UpdateAutoSize();
                }

                TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the icon texture name.
    /// </summary>
    public string? IconTextureName { get; set; }

    /// <summary>
    /// Gets or sets whether the button is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                UpdateState();
                EnabledChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets or sets the button width.
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
    /// Gets or sets the button height.
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
    /// Gets or sets the size mode (Fixed or Auto).
    /// </summary>
    public ButtonSizeMode SizeMode
    {
        get => _sizeMode;
        set
        {
            if (_sizeMode != value)
            {
                _sizeMode = value;

                if (_sizeMode == ButtonSizeMode.Auto)
                {
                    UpdateAutoSize();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the padding around the button content.
    /// </summary>
    public int Padding { get; set; } = DefaultPadding;

    /// <summary>
    /// Gets the current visual state of the button.
    /// </summary>
    public ButtonState CurrentState => _currentState;

    /// <summary>
    /// Event raised when the button is clicked.
    /// </summary>
    public event EventHandler? Click;

    /// <summary>
    /// Event raised when the mouse enters the button bounds.
    /// </summary>
    public event EventHandler? MouseEnter;

    /// <summary>
    /// Event raised when the mouse exits the button bounds.
    /// </summary>
    public event EventHandler? MouseExit;

    /// <summary>
    /// Event raised when the enabled state changes.
    /// </summary>
    public event EventHandler? EnabledChanged;

    /// <summary>
    /// Event raised when the text changes.
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    /// Event raised when the button state changes.
    /// </summary>
    public event EventHandler? StateChanged;

    public bool IsFocusable => IsEnabled;

    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus != value)
            {
                _hasFocus = value;
                UpdateState();
            }
        }
    }

    public Rectangle<int> Bounds
        => new(
            new((int)Transform.Position.X, (int)Transform.Position.Y),
            new((int)Transform.Size.X, (int)Transform.Size.Y)
        );

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus || !IsEnabled)
        {
            return;
        }

        // Handle Space or Enter to trigger click
        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Space) ||
            IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Enter))
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        var wasInBounds = _isMouseInBounds;
        _isMouseInBounds = IsMouseInBounds(mousePos);

        // Handle mouse enter/exit
        if (_isMouseInBounds && !wasInBounds)
        {
            MouseEnter?.Invoke(this, EventArgs.Empty);
        }
        else if (!_isMouseInBounds && wasInBounds)
        {
            MouseExit?.Invoke(this, EventArgs.Empty);
        }

        // Handle mouse down
        if (_isMouseInBounds && _inputManager.IsMouseButtonPressed(MouseButton.Left))
        {
            _isMouseDown = true;
        }

        // Handle mouse up (click)
        if (_isMouseDown && !_inputManager.IsMouseButtonDown(MouseButton.Left))
        {
            if (_isMouseInBounds)
            {
                Click?.Invoke(this, EventArgs.Empty);
            }

            _isMouseDown = false;
        }

        UpdateState();
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented for button
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

        var bounds = new Rectangle<float>(Transform.Position, new Vector2D<float>(Transform.Size.X, Transform.Size.Y));

        // Get colors based on current state
        var (bgColor, borderColor, textColor) = GetStateColors();

        // Background
        yield return DrawRectangle(bounds, bgColor, depth: NextDepth());

        // Border
        foreach (var cmd in DrawHollowRectangle(Transform.Position, new Vector2D<float>(Transform.Size.X, Transform.Size.Y), borderColor, Theme.BorderThickness, depth: NextDepth()))
        {
            yield return cmd;
        }

        // Calculate content position (centered)
        var contentWidth = CalculateContentWidth();
        var contentHeight = CalculateContentHeight();
        var contentX = Transform.Position.X + (Transform.Size.X - contentWidth) / 2f;
        var contentY = Transform.Position.Y + (Transform.Size.Y - contentHeight) / 2f;

        var currentX = contentX;

        // Icon
        if (!string.IsNullOrEmpty(IconTextureName))
        {
            var iconSize = Theme.FontSize;
            var iconPos = new Vector2D<float>(currentX, contentY);

            // Note: DrawTexture would need to be implemented in BaseGameObject2D
            // For now, we'll skip the icon rendering or you can add it later
            // yield return DrawTexture(IconTextureName, iconPos, new Vector2D<float>(iconSize, iconSize), depth: NextDepth());

            currentX += iconSize + IconTextSpacing;
        }

        // Text
        if (!string.IsNullOrEmpty(_text))
        {
            var textPos = new Vector2D<float>(
                currentX,
                Transform.Position.Y + (Transform.Size.Y - Theme.FontSize) / 2f
            );

            yield return DrawTextCustom(Theme.FontName, _text, Theme.FontSize, textPos, color: textColor, depth: NextDepth());
        }
    }

    private void UpdateTransformSize()
    {
        Transform.Size = new Vector2D<float>(_width, _height);
    }

    private void UpdateAutoSize()
    {
        if (_sizeMode != ButtonSizeMode.Auto)
        {
            return;
        }

        var contentWidth = CalculateContentWidth();
        var contentHeight = CalculateContentHeight();

        _width = (int)(contentWidth + Padding * 2);
        _height = (int)(contentHeight + Padding * 2);

        UpdateTransformSize();
    }

    private float CalculateContentWidth()
    {
        float width = 0;

        // Icon width
        if (!string.IsNullOrEmpty(IconTextureName))
        {
            width += Theme.FontSize;

            if (!string.IsNullOrEmpty(_text))
            {
                width += IconTextSpacing;
            }
        }

        // Text width
        if (!string.IsNullOrEmpty(_text))
        {
            width += TextMeasurement.MeasureStringWidth(_assetManager, _text, Theme.FontName, Theme.FontSize);
        }

        return width;
    }

    private float CalculateContentHeight()
    {
        return Theme.FontSize;
    }

    private void UpdateState()
    {
        var previousState = _currentState;

        if (!_isEnabled)
        {
            _currentState = ButtonState.Disabled;
        }
        else if (_isMouseDown && _isMouseInBounds)
        {
            _currentState = ButtonState.Pressed;
        }
        else if (_isMouseInBounds)
        {
            _currentState = ButtonState.Hover;
        }
        else
        {
            _currentState = ButtonState.Normal;
        }

        if (_currentState != previousState)
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private (Color4b bgColor, Color4b borderColor, Color4b textColor) GetStateColors()
    {
        return _currentState switch
        {
            ButtonState.Normal => (Theme.BackgroundColor, Theme.BorderColor, Theme.TextColor),
            ButtonState.Hover => (Theme.ItemHoveredColor, Theme.BorderColorFocused, Theme.TextColor),
            ButtonState.Pressed => (Theme.ItemSelectedColor, Theme.BorderColorFocused, Theme.TextColor),
            ButtonState.Disabled => (
                new Color4b((byte)(Theme.BackgroundColor.R * 0.5f), (byte)(Theme.BackgroundColor.G * 0.5f), (byte)(Theme.BackgroundColor.B * 0.5f), Theme.BackgroundColor.A),
                new Color4b((byte)(Theme.BorderColor.R * 0.5f), (byte)(Theme.BorderColor.G * 0.5f), (byte)(Theme.BorderColor.B * 0.5f), Theme.BorderColor.A),
                new Color4b((byte)(Theme.TextColor.R * 0.5f), (byte)(Theme.TextColor.G * 0.5f), (byte)(Theme.TextColor.B * 0.5f), Theme.TextColor.A)
            ),
            _ => (Theme.BackgroundColor, Theme.BorderColor, Theme.TextColor)
        };
    }

    private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Key key)
    {
        return current.IsKeyPressed(key) && !previous.IsKeyPressed(key);
    }
}
