using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.GameObjects.Utils;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.Utils;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Input;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Utils;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using TrippyGL;
using MouseButton = Lilly.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// A text input control that supports editing, selection, cursor positioning, and basic text operations.
/// Similar to a standard text box or input field.
/// </summary>
public class TextEditGameObject : Base2dGameObject, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private string _text = string.Empty;
    private int _cursorPosition;
    private int _selectionStart;
    private int _selectionLength;
    private bool _cursorVisible = true;
    private double _lastBlinkTime;
    private float _scrollOffset;
    private bool _isDragging;
    private int _dragStartIndex;
    private bool _hasFocus;
    private readonly int _width;
    private readonly int _height;

    private const double CursorBlinkInterval = 0.5; // 500ms
    private const int Padding = 4;

    /// <summary>
    /// Initializes a new instance of the TextEditGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    /// <param name="assetManager">The asset manager for loading fonts.</param>
    /// <param name="theme">The UI theme to use for styling.</param>
    /// <param name="gameObjectManager">The game object manager.</param>
    public TextEditGameObject(
        IInputManagerService inputManager,
        IAssetManager assetManager,
        UITheme theme,
        IRenderPipeline gameObjectManager
    ) : base("TextEdit", gameObjectManager, 100)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        _width = 200;
        _height = 32;
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));
        UpdateTransformSize();
    }

    public int MaxLength { get; set; } = 256;
    public bool IsReadOnly { get; set; }
    public bool IsPassword { get; set; }
    public string PlaceholderText { get; set; } = string.Empty;

    public UITheme Theme { get; }

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

    public Vector2 Bounds => new(Transform.Size.X, Transform.Size.Y);

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus || IsReadOnly)
        {
            return;
        }

        // Keys that should repeat
        if (_inputManager.IsKeyRepeated(Key.Backspace))
        {
            HandleBackspace();
            ResetCursorBlink(gameTime);
        }
        else if (_inputManager.IsKeyRepeated(Key.Delete))
        {
            HandleDelete();
            ResetCursorBlink(gameTime);
        }
        else if (_inputManager.IsKeyRepeated(Key.Left))
        {
            HandleLeftArrow(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (_inputManager.IsKeyRepeated(Key.Right))
        {
            HandleRightArrow(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (_inputManager.IsKeyRepeated(Key.Home))
        {
            HandleHome(keyboardState);
            ResetCursorBlink(gameTime);
        }
        else if (_inputManager.IsKeyRepeated(Key.End))
        {
            HandleEnd(keyboardState);
            ResetCursorBlink(gameTime);
        }

        // Keys that should NOT repeat
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
            _cursorPosition = TextMeasurement.GetCharacterIndexAtPosition(
                _assetManager,
                GetDisplayText(),
                localX,
                Theme.FontName,
                Theme.FontSize
            );
            _dragStartIndex = _cursorPosition;
            _isDragging = true;
            ClearSelection();
            ResetCursorBlink(gameTime);
        }

        if (_isDragging && _inputManager.IsMouseButtonDown(MouseButton.Left))
        {
            var localX = mousePos.X - Transform.Position.X - Padding + _scrollOffset;
            var currentIndex = TextMeasurement.GetCharacterIndexAtPosition(
                _assetManager,
                GetDisplayText(),
                localX,
                Theme.FontName,
                Theme.FontSize
            );

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
        var boundsX = (int)mousePosition.X >= (int)Transform.Position.X &&
                      (int)mousePosition.X <= (int)(Transform.Position.X + Transform.Size.X);
        var boundsY = (int)mousePosition.Y >= (int)Transform.Position.Y &&
                      (int)mousePosition.Y <= (int)(Transform.Position.Y + Transform.Size.Y);

        return boundsX && boundsY;
    }

    protected override void OnDraw(GameTime gameTime)
    {
        if (!IsActive || SpriteBatcher == null)
        {
            return;
        }

        // Background
        var bgColor = HasFocus ? Theme.BackgroundColorFocused : Theme.BackgroundColor;
        var backgroundPos = Transform.Position;
        var backgroundSize = new Vector2(Transform.Size.X, Transform.Size.Y);
        SpriteBatcher.DrawRectangle(backgroundPos, backgroundSize, bgColor);

        // Border
        var borderColor = HasFocus ? Theme.BorderColorFocused : Theme.BorderColor;
        SpriteBatcher.DrawHollowRectangle(
            Transform.Position,
            new Vector2(Transform.Size.X, Transform.Size.Y),
            borderColor,
            Theme.BorderThickness
        );

        // Selection highlight
        if (_selectionLength > 0 && HasFocus)
        {
            var (selectionPos, selectionSize) = GetSelectionRectangle();
            var selectionColor = new Color4b(
                Theme.ItemSelectedColor.R,
                Theme.ItemSelectedColor.G,
                Theme.ItemSelectedColor.B,
                128
            );
            SpriteBatcher.DrawRectangle(selectionPos, selectionSize, selectionColor);
        }

        // Text content or placeholder
        var displayText = GetDisplayText();

        if (!string.IsNullOrEmpty(displayText))
        {
            var textPos = new Vector2(
                Transform.Position.X + Padding - _scrollOffset,
                Transform.Position.Y + (Transform.Size.Y - Theme.FontSize) / 2f
            );

            SpriteBatcher.DrawText(
                Theme.FontName,
                Theme.FontSize,
                displayText,
                textPos,
                Theme.TextColor
            );
        }
        else if (!HasFocus && !string.IsNullOrEmpty(PlaceholderText))
        {
            var textPos = new Vector2(
                Transform.Position.X + Padding,
                Transform.Position.Y + (Transform.Size.Y - Theme.FontSize) / 2f
            );

            SpriteBatcher.DrawText(
                Theme.FontName,
                Theme.FontSize,
                PlaceholderText,
                textPos,
                Theme.PlaceholderTextColor
            );
        }

        // Cursor
        if (HasFocus && _selectionLength == 0)
        {
            UpdateCursorBlink(gameTime);

            if (_cursorVisible)
            {
                var (cursorPos, cursorSize) = GetCursorRectangle();
                SpriteBatcher.DrawRectangle(cursorPos, cursorSize, Theme.BorderColorFocused);
            }
        }
    }

    private void ClearSelection()
    {
        _selectionStart = 0;
        _selectionLength = 0;
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

    private (Vector2 position, Vector2 size) GetCursorRectangle()
    {
        var displayText = GetDisplayText();
        var textBeforeCursor = _cursorPosition > 0 ? displayText[.._cursorPosition] : string.Empty;
        var cursorX = TextMeasurement.MeasureStringWidth(_assetManager, textBeforeCursor, Theme.FontName, Theme.FontSize);

        var position = new Vector2(
            Transform.Position.X + Padding + cursorX - _scrollOffset,
            Transform.Position.Y + 4
        );
        var size = new Vector2(2, Transform.Size.Y - 8);

        return (position, size);
    }

    private string GetDisplayText()
    {
        if (IsPassword && !string.IsNullOrEmpty(_text))
        {
            return new('•', _text.Length);
        }

        return _text;
    }

    private (Vector2 position, Vector2 size) GetSelectionRectangle()
    {
        var displayText = GetDisplayText();
        var textBeforeSelection = displayText[.._selectionStart];
        var selectedText = displayText.Substring(_selectionStart, _selectionLength);

        var startX = TextMeasurement.MeasureStringWidth(_assetManager, textBeforeSelection, Theme.FontName, Theme.FontSize);
        var width = TextMeasurement.MeasureStringWidth(_assetManager, selectedText, Theme.FontName, Theme.FontSize);

        var position = new Vector2(
            Transform.Position.X + Padding + startX - _scrollOffset,
            Transform.Position.Y + 2
        );
        var size = new Vector2(width, Transform.Size.Y - 4);

        return (position, size);
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

    private void HandleCharacterInput(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        var shift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);

        foreach (var key in keyboardState.GetPressedKeys())
        {
            if (!previousKeyboardState.IsKeyPressed(key))
            {
                var character = KeyboardInputUtils.KeyToChar(key, shift);

                if (character != null)
                {
                    InsertCharacter(character.Value);
                    ResetCursorBlink(gameTime);
                }
            }
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

    private static bool IsControlDown(KeyboardState keyboardState)
        => keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);

    private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Key key)
        => current.IsKeyPressed(key) && !previous.IsKeyPressed(key);

    private void ResetCursorBlink(GameTime gameTime)
    {
        _cursorVisible = true;
        _lastBlinkTime = gameTime.GetTotalGameTimeSeconds();
    }

    private void SelectAll()
    {
        _selectionStart = 0;
        _selectionLength = _text.Length;
        _cursorPosition = _text.Length;
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

    private void UpdateScrollOffset()
    {
        var displayText = GetDisplayText();
        var textBeforeCursor = _cursorPosition > 0 ? displayText[.._cursorPosition] : string.Empty;
        var cursorX = TextMeasurement.MeasureStringWidth(_assetManager, textBeforeCursor, Theme.FontName, Theme.FontSize);
        var visibleWidth = Transform.Size.X - Padding * 2;

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

    private void UpdateTransformSize()
    {
        Transform.Size = new(_width, _height);
    }
}
