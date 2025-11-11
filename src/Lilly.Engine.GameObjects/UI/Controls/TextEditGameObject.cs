using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Rendering;
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
/// A text input control that supports editing, selection, cursor positioning, and basic text operations.
/// Similar to a standard text box or input field.
/// </summary>
public class TextEditGameObject : BaseGameObject2D, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;
    private readonly UITheme _theme;

    private string _text = string.Empty;
    private int _cursorPosition = 0;
    private int _selectionStart = 0;
    private int _selectionLength = 0;
    private bool _cursorVisible = true;
    private double _lastBlinkTime = 0;
    private float _scrollOffset = 0;
    private bool _isDragging = false;
    private int _dragStartIndex = 0;
    private bool _hasFocus;

    private const double CursorBlinkInterval = 0.5; // 500ms
    private const int Padding = 4;

    /// <summary>
    /// Initializes a new instance of the TextEditGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    /// <param name="assetManager">The asset manager for loading fonts.</param>
    /// <param name="theme">The UI theme to use for styling.</param>
    public TextEditGameObject(IInputManagerService inputManager, IAssetManager assetManager, UITheme theme)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
    }

    public int MaxLength { get; set; } = 256;
    public bool IsReadOnly { get; set; } = false;
    public bool IsPassword { get; set; } = false;
    public string PlaceholderText { get; set; } = string.Empty;
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 32;
    public UITheme Theme => _theme;

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;

                if (_text.Length > MaxLength)
                {
                    _text = _text[..MaxLength];
                }

                _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                ClearSelection();
                TextChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? TextChanged;
    public event EventHandler? EnterPressed;

    public bool IsFocusable => true;

    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus != value)
            {
                _hasFocus = value;

                if (_hasFocus)
                {
                    _cursorVisible = true;
                    _lastBlinkTime = 0;
                }
                else
                {
                    ClearSelection();
                    _isDragging = false;
                }
            }
        }
    }

    public Rectangle<int> Bounds
        => new(
            new((int)Transform.Position.X, (int)Transform.Position.Y),
            new(Width, Height)
        );

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus || IsReadOnly)
        {
            return;
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Backspace))
        {
            HandleBackspace();
            ResetCursorBlink(gameTime);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Delete))
        {
            HandleDelete();
            ResetCursorBlink(gameTime);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Left))
        {
            HandleLeftArrow(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Right))
        {
            HandleRightArrow(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Home))
        {
            HandleHome(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.End))
        {
            HandleEnd(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Enter))
        {
            EnterPressed?.Invoke(this, EventArgs.Empty);
        }
        else if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.A) && IsControlDown(keyboardState))
        {
            SelectAll();
            ResetCursorBlink(gameTime);
        }
        else
        {
            HandleCharacterInput(keyboardState, previousKeyboardState, gameTime);
        }

        UpdateScrollOffset();
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);

        if (!IsMouseInBounds(mousePos))
        {
            _isDragging = false;
            return;
        }

        if (_inputManager.IsMouseButtonPressed(MouseButton.Left))
        {
            var localX = mousePos.X - Transform.Position.X - Padding + _scrollOffset;
            _cursorPosition = TextMeasurement.GetCharacterIndexAtPosition(_assetManager, GetDisplayText(), localX, _theme.FontName, _theme.FontSize);
            _dragStartIndex = _cursorPosition;
            _isDragging = true;
            ClearSelection();
            ResetCursorBlink(gameTime);
        }

        if (_isDragging && _inputManager.IsMouseButtonDown(MouseButton.Left))
        {
            var localX = mousePos.X - Transform.Position.X - Padding + _scrollOffset;
            var currentIndex = TextMeasurement.GetCharacterIndexAtPosition(_assetManager, GetDisplayText(), localX, _theme.FontName, _theme.FontSize);

            if (currentIndex != _dragStartIndex)
            {
                _selectionStart = Math.Min(_dragStartIndex, currentIndex);
                _selectionLength = Math.Abs(currentIndex - _dragStartIndex);
                _cursorPosition = currentIndex;
            }
            else
            {
                ClearSelection();
            }
        }

        if (!_inputManager.IsMouseButtonDown(MouseButton.Left))
        {
            _isDragging = false;
        }
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented for text edit
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

        var bounds = new Rectangle<float>(Transform.Position, new Vector2D<float>(Width, Height));

        // Background
        var bgColor = HasFocus ? _theme.BackgroundColorFocused : _theme.BackgroundColor;
        yield return DrawRectangle(bounds, bgColor, depth: 0.50f);

        // Border
        var borderColor = HasFocus ? _theme.BorderColorFocused : _theme.BorderColor;
        foreach (var cmd in DrawHollowRectangle(Transform.Position, new Vector2D<float>(Width, Height), borderColor, _theme.BorderThickness))
        {
            yield return cmd;
        }

        // Selection highlight
        if (_selectionLength > 0 && HasFocus)
        {
            var selectionRect = GetSelectionRectangle();
            var selectionColor = new Color4b(_theme.ItemSelectedColor.R, _theme.ItemSelectedColor.G, _theme.ItemSelectedColor.B, 128);
            yield return DrawRectangle(selectionRect, selectionColor, depth: 0.51f);
        }

        // Text content or placeholder
        var displayText = GetDisplayText();

        if (!string.IsNullOrEmpty(displayText))
        {
            var textPos = new Vector2D<float>(
                Transform.Position.X + Padding - _scrollOffset,
                Transform.Position.Y + (Height - _theme.FontSize) / 2f
            );

            yield return DrawTextCustom(_theme.FontName, displayText, _theme.FontSize, textPos, color: _theme.TextColor, depth: 0.52f);
        }
        else if (!HasFocus && !string.IsNullOrEmpty(PlaceholderText))
        {
            var textPos = new Vector2D<float>(
                Transform.Position.X + Padding,
                Transform.Position.Y + (Height - _theme.FontSize) / 2f
            );

            yield return DrawTextCustom(_theme.FontName, PlaceholderText, _theme.FontSize, textPos, color: _theme.PlaceholderTextColor, depth: 0.52f);
        }

        // Cursor
        if (HasFocus && _selectionLength == 0)
        {
            UpdateCursorBlink(gameTime);

            if (_cursorVisible)
            {
                var cursorRect = GetCursorRectangle();
                yield return DrawRectangle(cursorRect, _theme.BorderColorFocused, depth: 0.53f);
            }
        }
    }

    private void HandleBackspace()
    {
        if (_selectionLength > 0)
        {
            DeleteSelection();
        }
        else if (_cursorPosition > 0)
        {
            _text = _text.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleDelete()
    {
        if (_selectionLength > 0)
        {
            DeleteSelection();
        }
        else if (_cursorPosition < _text.Length)
        {
            _text = _text.Remove(_cursorPosition, 1);
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void HandleLeftArrow(KeyboardState keyboardState)
    {
        var isShiftDown = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);

        if (isShiftDown)
        {
            if (_selectionLength == 0)
            {
                _selectionStart = _cursorPosition;
            }

            if (_cursorPosition > 0)
            {
                _cursorPosition--;

                if (_cursorPosition < _selectionStart)
                {
                    _selectionLength = _selectionStart - _cursorPosition;
                    _selectionStart = _cursorPosition;
                }
                else
                {
                    _selectionLength--;
                }
            }
        }
        else
        {
            if (_selectionLength > 0)
            {
                _cursorPosition = _selectionStart;
                ClearSelection();
            }
            else if (_cursorPosition > 0)
            {
                _cursorPosition--;
            }
        }
    }

    private void HandleRightArrow(KeyboardState keyboardState)
    {
        var isShiftDown = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);

        if (isShiftDown)
        {
            if (_selectionLength == 0)
            {
                _selectionStart = _cursorPosition;
            }

            if (_cursorPosition < _text.Length)
            {
                _cursorPosition++;
                _selectionLength = _cursorPosition - _selectionStart;
            }
        }
        else
        {
            if (_selectionLength > 0)
            {
                _cursorPosition = _selectionStart + _selectionLength;
                ClearSelection();
            }
            else if (_cursorPosition < _text.Length)
            {
                _cursorPosition++;
            }
        }
    }

    private void HandleHome(KeyboardState keyboardState)
    {
        var isShiftDown = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);

        if (isShiftDown)
        {
            if (_selectionLength == 0)
            {
                _selectionStart = 0;
                _selectionLength = _cursorPosition;
            }
            else
            {
                _selectionLength = _selectionStart + _selectionLength;
                _selectionStart = 0;
            }
        }
        else
        {
            ClearSelection();
        }

        _cursorPosition = 0;
    }

    private void HandleEnd(KeyboardState keyboardState)
    {
        var isShiftDown = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);

        if (isShiftDown)
        {
            if (_selectionLength == 0)
            {
                _selectionStart = _cursorPosition;
            }

            _selectionLength = _text.Length - _selectionStart;
        }
        else
        {
            ClearSelection();
        }

        _cursorPosition = _text.Length;
    }

    private void HandleCharacterInput(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (!previousKeyboardState.IsKeyPressed(key))
            {
                var character = KeyToChar(key, keyboardState);

                if (character != null)
                {
                    InsertCharacter(character.Value);
                    ResetCursorBlink(gameTime);
                }
            }
        }
    }

    private char? KeyToChar(Key key, KeyboardState keyboardState)
    {
        var isShiftDown = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);

        if (key >= Key.A && key <= Key.Z)
        {
            var offset = key - Key.A;
            return isShiftDown ? (char)('A' + offset) : (char)('a' + offset);
        }

        if (key >= Key.Number0 && key <= Key.Number9)
        {
            if (isShiftDown)
            {
                return key switch
                {
                    Key.Number1 => '!',
                    Key.Number2 => '@',
                    Key.Number3 => '#',
                    Key.Number4 => '$',
                    Key.Number5 => '%',
                    Key.Number6 => '^',
                    Key.Number7 => '&',
                    Key.Number8 => '*',
                    Key.Number9 => '(',
                    Key.Number0 => ')',
                    _ => null
                };
            }

            var offset = key - Key.Number0;
            return (char)('0' + offset);
        }

        if (key == Key.Space)
        {
            return ' ';
        }

        return key switch
        {
            Key.Minus => isShiftDown ? '_' : '-',
            Key.Equal => isShiftDown ? '+' : '=',
            Key.LeftBracket => isShiftDown ? '{' : '[',
            Key.RightBracket => isShiftDown ? '}' : ']',
            Key.Semicolon => isShiftDown ? ':' : ';',
            Key.Apostrophe => isShiftDown ? '"' : '\'',
            Key.Comma => isShiftDown ? '<' : ',',
            Key.Period => isShiftDown ? '>' : '.',
            Key.Slash => isShiftDown ? '?' : '/',
            Key.BackSlash => isShiftDown ? '|' : '\\',
            Key.GraveAccent => isShiftDown ? '~' : '`',
            _ => null
        };
    }

    private void InsertCharacter(char character)
    {
        if (_selectionLength > 0)
        {
            DeleteSelection();
        }

        if (_text.Length >= MaxLength)
        {
            return;
        }

        _text = _text.Insert(_cursorPosition, character.ToString());
        _cursorPosition++;
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteSelection()
    {
        if (_selectionLength == 0)
        {
            return;
        }

        _text = _text.Remove(_selectionStart, _selectionLength);
        _cursorPosition = _selectionStart;
        ClearSelection();
        TextChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SelectAll()
    {
        _selectionStart = 0;
        _selectionLength = _text.Length;
        _cursorPosition = _text.Length;
    }

    private void ClearSelection()
    {
        _selectionStart = 0;
        _selectionLength = 0;
    }

    private string GetDisplayText()
    {
        if (IsPassword && !string.IsNullOrEmpty(_text))
        {
            return new string('•', _text.Length);
        }

        return _text;
    }

    private Rectangle<float> GetSelectionRectangle()
    {
        var displayText = GetDisplayText();
        var textBeforeSelection = displayText[.._selectionStart];
        var selectedText = displayText.Substring(_selectionStart, _selectionLength);

        var startX = TextMeasurement.MeasureStringWidth(_assetManager, textBeforeSelection, _theme.FontName, _theme.FontSize);
        var width = TextMeasurement.MeasureStringWidth(_assetManager, selectedText, _theme.FontName, _theme.FontSize);

        return new Rectangle<float>(
            new Vector2D<float>(Transform.Position.X + Padding + startX - _scrollOffset, Transform.Position.Y + 2),
            new Vector2D<float>(width, Height - 4)
        );
    }

    private Rectangle<float> GetCursorRectangle()
    {
        var displayText = GetDisplayText();
        var textBeforeCursor = _cursorPosition > 0 ? displayText[.._cursorPosition] : string.Empty;
        var cursorX = TextMeasurement.MeasureStringWidth(_assetManager, textBeforeCursor, _theme.FontName, _theme.FontSize);

        return new Rectangle<float>(
            new Vector2D<float>(Transform.Position.X + Padding + cursorX - _scrollOffset, Transform.Position.Y + 4),
            new Vector2D<float>(2, Height - 8)
        );
    }

    private void UpdateScrollOffset()
    {
        var displayText = GetDisplayText();
        var textBeforeCursor = _cursorPosition > 0 ? displayText[.._cursorPosition] : string.Empty;
        var cursorX = TextMeasurement.MeasureStringWidth(_assetManager, textBeforeCursor, _theme.FontName, _theme.FontSize);
        var visibleWidth = Width - Padding * 2;

        if (cursorX - _scrollOffset > visibleWidth)
        {
            _scrollOffset = cursorX - visibleWidth;
        }

        if (cursorX - _scrollOffset < 0)
        {
            _scrollOffset = cursorX;
        }

        if (_scrollOffset < 0)
        {
            _scrollOffset = 0;
        }
    }

    private void UpdateCursorBlink(GameTime gameTime)
    {
        var currentTime = gameTime.GetTotalGameTimeSeconds();
        var elapsed = currentTime - _lastBlinkTime;

        if (elapsed >= CursorBlinkInterval)
        {
            _cursorVisible = !_cursorVisible;
            _lastBlinkTime = currentTime;
        }
    }

    private void ResetCursorBlink(GameTime gameTime)
    {
        _cursorVisible = true;
        _lastBlinkTime = gameTime.GetTotalGameTimeSeconds();
    }

    private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Key key)
    {
        return current.IsKeyPressed(key) && !previous.IsKeyPressed(key);
    }

    private static bool IsControlDown(KeyboardState keyboardState)
    {
        return keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
    }
}
