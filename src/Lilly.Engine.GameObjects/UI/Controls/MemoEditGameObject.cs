using System.Numerics;
using System.Text;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.GameObjects.Utils;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// Multi-line text editor UI component with scrolling, selection, and rich keyboard navigation.
/// </summary>
public class MemoEditGameObject : BaseGameObject2D, IInputReceiver
{
    private const double CursorBlinkInterval = 0.5;
    private const int ScrollbarWidth = 16;
    private const int BorderThickness = 2;
    private const int Padding = 4;

    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private readonly List<string> _lines = new() { string.Empty };

    private string _text = string.Empty;
    private float _lineHeight = 20;
    private float _charWidth = 10;
    private int _maxVisibleLines = 10;
    private int _maxVisibleCharsPerLine = 40;

    private int _cursorLine;
    private int _cursorColumn;
    private double _cursorBlinkTimer;
    private bool _cursorVisible = true;

    private int _selectionStartLine;
    private int _selectionStartColumn;
    private int _selectionEndLine;
    private int _selectionEndColumn;

    private bool _isSelectingWithMouse;

    private int _scrollOffsetX;
    private int _scrollOffsetY;

    private int? _maxLength;
    private int _longestLineLength;

    private int _width = 420;
    private int _height = 260;
    private bool _hasFocus;
    private bool _isMouseInBounds;
    private int _hoveredIndex = -1;

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            var newText = value ?? string.Empty;

            if (_maxLength.HasValue && newText.Length > _maxLength.Value)
            {
                newText = newText.Substring(0, _maxLength.Value);
            }

            if (_text != newText)
            {
                var oldText = _text;
                SetTextInternal(newText);
                TextChanged?.Invoke(this, new(oldText, _text));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the memo is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the maximum text length.
    /// </summary>
    public int? MaxLength
    {
        get => _maxLength;
        set
        {
            _maxLength = value;

            if (_maxLength.HasValue && _text.Length > _maxLength.Value)
            {
                Text = _text.Substring(0, _maxLength.Value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the cursor line position.
    /// </summary>
    public int CursorLine
    {
        get => _cursorLine;
        set => SetCursorPosition(value, _cursorColumn);
    }

    /// <summary>
    /// Gets or sets the cursor column position.
    /// </summary>
    public int CursorColumn
    {
        get => _cursorColumn;
        set => SetCursorPosition(_cursorLine, value);
    }

    /// <summary>
    /// Gets whether there is a current selection.
    /// </summary>
    public bool HasSelection { get; private set; }

    /// <summary>
    /// Gets the selected text.
    /// </summary>
    public string SelectedText
        => HasSelection
               ? GetTextInRange(_selectionStartLine, _selectionStartColumn, _selectionEndLine, _selectionEndColumn)
               : string.Empty;

    /// <summary>
    /// Gets the line count.
    /// </summary>
    public int LineCount => _lines.Count;

    /// <summary>
    /// Gets or sets the UI theme.
    /// </summary>
    public UITheme Theme { get; }

    /// <summary>
    /// Gets or sets the width.
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
                UpdateMeasurements();
            }
        }
    }

    /// <summary>
    /// Gets or sets the height.
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
                UpdateMeasurements();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show line numbers.
    /// </summary>
    public bool ShowLineNumbers { get; set; }

    /// <summary>
    /// Gets or sets the tab size.
    /// </summary>
    public int TabSize { get; set; } = 4;

    /// <summary>
    /// Event raised when text changes.
    /// </summary>
    public event EventHandler<TextChangedEventArgs>? TextChanged;

    /// <summary>
    /// Event raised when focus is gained.
    /// </summary>
    public event EventHandler? GotFocus;

    /// <summary>
    /// Event raised when focus is lost.
    /// </summary>
    public event EventHandler? LostFocus;

    /// <summary>
    /// Event raised when cursor position changes.
    /// </summary>
    public event EventHandler? CursorPositionChanged;

    /// <summary>
    /// Event raised when selection changes.
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Initializes a new instance of MemoEditGameObject.
    /// </summary>
    public MemoEditGameObject(IInputManagerService inputManager, IAssetManager assetManager, UITheme theme)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));

        UpdateTransformSize();
        UpdateMeasurements();
    }

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
                    _cursorBlinkTimer = 0;
                    _cursorVisible = true;
                    GotFocus?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _cursorVisible = false;
                    _isSelectingWithMouse = false;
                    LostFocus?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    public Rectangle<int> Bounds
        => new(
            new((int)Transform.Position.X, (int)Transform.Position.Y),
            new(_width, _height)
        );

    /// <summary>
    /// Clears the current selection.
    /// </summary>
    public void ClearSelection()
    {
        if (HasSelection)
        {
            HasSelection = false;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        _selectionStartLine = _selectionEndLine = _cursorLine;
        _selectionStartColumn = _selectionEndColumn = _cursorColumn;
    }

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus || IsReadOnly)
        {
            return;
        }

        var isControlPressed = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);

        // Define keys that should NOT repeat
        var noRepeatKeys = new HashSet<Key> { Key.Enter };

        // Add Ctrl+A to no-repeat (only if Ctrl is pressed)
        if (isControlPressed && _inputManager.IsKeyPressed(Key.A))
        {
            ProcessKey(Key.A, keyboardState);

            return;
        }

        var pressedKeys = keyboardState.GetPressedKeys();

        foreach (var key in pressedKeys)
        {
            // Skip Ctrl, Shift, Alt keys themselves
            if (key == Key.ControlLeft ||
                key == Key.ControlRight ||
                key == Key.ShiftLeft ||
                key == Key.ShiftRight ||
                key == Key.AltLeft ||
                key == Key.AltRight)
            {
                continue;
            }

            // For keys that should not repeat, only process on initial press
            if (noRepeatKeys.Contains(key))
            {
                if (!previousKeyboardState.IsKeyPressed(key))
                {
                    ProcessKey(key, keyboardState);
                }
            }

            // For all other keys, use repeat logic
            else if (_inputManager.IsKeyRepeated(key))
            {
                ProcessKey(key, keyboardState);
            }
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        var wasInBounds = _isMouseInBounds;
        _isMouseInBounds = IsMouseInBounds(mousePos);

        if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && _isMouseInBounds)
        {
            if (!HasFocus)
            {
                HasFocus = true;
            }

            BeginMouseSelection(mousePos);
        }
        else if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && !_isMouseInBounds && HasFocus)
        {
            HasFocus = false;
            ClearSelection();
        }

        if (_isSelectingWithMouse && _inputManager.IsMouseButtonDown(MouseButton.Left))
        {
            UpdateMouseSelection(mousePos);
        }
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented for memo
    }

    /// <summary>
    /// Inserts text at cursor position.
    /// </summary>
    public void InsertText(string text)
    {
        if (IsReadOnly || string.IsNullOrEmpty(text))
        {
            return;
        }

        DeleteSelection();

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        if (_maxLength.HasValue)
        {
            var available = _maxLength.Value - GetTotalTextLength();

            if (available <= 0)
            {
                return;
            }

            if (normalized.Length > available)
            {
                normalized = normalized.Substring(0, available);
            }
        }

        var linesToInsert = normalized.Split('\n');
        var currentLine = _lines[_cursorLine];
        var before = currentLine.Substring(0, _cursorColumn);
        var after = currentLine.Substring(_cursorColumn);

        if (linesToInsert.Length == 1)
        {
            _lines[_cursorLine] = before + linesToInsert[0] + after;
            SetCursorPosition(_cursorLine, _cursorColumn + linesToInsert[0].Length);
        }
        else
        {
            _lines[_cursorLine] = before + linesToInsert[0];

            for (var i = 1; i < linesToInsert.Length - 1; i++)
            {
                _lines.Insert(_cursorLine + i, linesToInsert[i]);
            }

            var lastIndex = _cursorLine + linesToInsert.Length - 1;
            _lines.Insert(lastIndex, linesToInsert[^1] + after);
            SetCursorPosition(lastIndex, linesToInsert[^1].Length);
        }

        UpdateTextFromLines();
    }

    public bool IsMouseInBounds(Vector2 mousePosition)
    {
        var bounds = Bounds;

        return bounds.Contains(new Vector2D<int>((int)mousePosition.X, (int)mousePosition.Y));
    }

    /// <summary>
    /// Selects all text.
    /// </summary>
    public void SelectAll()
    {
        _selectionStartLine = 0;
        _selectionStartColumn = 0;
        _selectionEndLine = _lines.Count - 1;
        _selectionEndColumn = _lines[^1].Length;
        HasSelection = true;
        SetCursorPosition(_selectionEndLine, _selectionEndColumn);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        var bounds = Bounds;

        // Cursor blinks every few frames (simplified without gameTime)
        _cursorBlinkTimer += 0.016; // ~60fps delta time

        if (_cursorBlinkTimer >= CursorBlinkInterval)
        {
            _cursorBlinkTimer = 0;
            _cursorVisible = !_cursorVisible;
        }

        // Draw background
        yield return DrawRectangle(
            new(Transform.Position, new(_width, _height)),
            Theme.BackgroundColor,
            NextDepth()
        );

        // Draw border
        var borderColor = HasFocus ? Theme.BorderColorFocused : Theme.BorderColor;

        foreach (var cmd in DrawHollowRectangle(
                     Transform.Position,
                     new(_width, _height),
                     borderColor,
                     BorderThickness,
                     NextDepth()
                 ))
        {
            yield return cmd;
        }

        // Draw text content
        var contentArea = GetContentArea();
        var startLine = _scrollOffsetY;
        var endLine = Math.Min(_lines.Count, startLine + _maxVisibleLines + 1);

        // Draw selection
        if (HasSelection)
        {
            foreach (var cmd in DrawSelection(contentArea, startLine, endLine))
            {
                yield return cmd;
            }
        }

        // Draw text
        for (var i = startLine; i < endLine; i++)
        {
            var line = _lines[i];

            if (line.Length > 0)
            {
                var visibleStart = Math.Max(0, _scrollOffsetX);
                var visibleEnd = Math.Min(line.Length, visibleStart + _maxVisibleCharsPerLine + 2);

                if (visibleEnd > visibleStart)
                {
                    var visibleText = line.Substring(visibleStart, visibleEnd - visibleStart);
                    var textPos = new Vector2D<float>(
                        contentArea.Origin.X,
                        contentArea.Origin.Y + (i - startLine) * _lineHeight
                    );

                    yield return DrawTextCustom(
                        Theme.FontName,
                        visibleText,
                        Theme.FontSize,
                        textPos,
                        color: Theme.TextColor,
                        depth: NextDepth()
                    );
                }
            }
        }

        // Draw cursor
        if (HasFocus && _cursorVisible)
        {
            var cursorX = contentArea.Origin.X + (_cursorColumn - _scrollOffsetX) * _charWidth;
            var cursorY = contentArea.Origin.Y + (_cursorLine - _scrollOffsetY) * _lineHeight;

            if (cursorX >= contentArea.Origin.X &&
                cursorX <= contentArea.Origin.X + contentArea.Size.X &&
                cursorY >= contentArea.Origin.Y &&
                cursorY <= contentArea.Origin.Y + contentArea.Size.Y)
            {
                yield return DrawRectangle(
                    new(
                        new(cursorX, cursorY),
                        new(2, _lineHeight)
                    ),
                    Theme.TextColor,
                    NextDepth()
                );
            }
        }
    }

    private void Backspace()
    {
        if (HasSelection)
        {
            DeleteSelection();

            return;
        }

        if (_cursorColumn > 0)
        {
            var line = _lines[_cursorLine];
            _lines[_cursorLine] = line.Remove(_cursorColumn - 1, 1);
            SetCursorPosition(_cursorLine, _cursorColumn - 1);
        }
        else if (_cursorLine > 0)
        {
            var previousLength = _lines[_cursorLine - 1].Length;
            _lines[_cursorLine - 1] += _lines[_cursorLine];
            _lines.RemoveAt(_cursorLine);
            SetCursorPosition(_cursorLine - 1, previousLength);
        }

        UpdateTextFromLines();
    }

    private void BeginMouseSelection(Vector2 mousePosition)
    {
        var (line, column) = GetPositionFromMouse(mousePosition);
        SetCursorPosition(line, column);
        StartSelection();
        _isSelectingWithMouse = true;
    }

    private void Delete()
    {
        if (HasSelection)
        {
            DeleteSelection();

            return;
        }

        var line = _lines[_cursorLine];

        if (_cursorColumn < line.Length)
        {
            _lines[_cursorLine] = line.Remove(_cursorColumn, 1);
        }
        else if (_cursorLine < _lines.Count - 1)
        {
            _lines[_cursorLine] += _lines[_cursorLine + 1];
            _lines.RemoveAt(_cursorLine + 1);
        }

        UpdateTextFromLines();
    }

    private void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        var (startLine, startColumn, endLine, endColumn) = GetNormalizedSelection();

        var before = _lines[startLine].Substring(0, startColumn);
        var after = _lines[endLine].Substring(endColumn);

        for (var i = endLine; i > startLine; i--)
        {
            _lines.RemoveAt(i);
        }

        _lines[startLine] = before + after;

        SetCursorPosition(startLine, startColumn);
        ClearSelection();
        UpdateTextFromLines();
    }

    private IEnumerable<RenderCommand> DrawSelection(Rectangle<float> contentArea, int startLine, int endLine)
    {
        var (selStartLine, selStartCol, selEndLine, selEndCol) = GetNormalizedSelection();

        for (var line = Math.Max(startLine, selStartLine); line <= Math.Min(endLine - 1, selEndLine); line++)
        {
            var lineY = contentArea.Origin.Y + (line - startLine) * _lineHeight;
            float selectionStartX;
            float selectionWidth;

            if (line == selStartLine && line == selEndLine)
            {
                selectionStartX = contentArea.Origin.X + Math.Max(0, selStartCol - _scrollOffsetX) * _charWidth;
                selectionWidth = (selEndCol - selStartCol) * _charWidth;
            }
            else if (line == selStartLine)
            {
                selectionStartX = contentArea.Origin.X + Math.Max(0, selStartCol - _scrollOffsetX) * _charWidth;
                selectionWidth = contentArea.Origin.X + contentArea.Size.X - selectionStartX;
            }
            else if (line == selEndLine)
            {
                selectionStartX = contentArea.Origin.X;
                selectionWidth = Math.Max(0, selEndCol - _scrollOffsetX) * _charWidth;
            }
            else
            {
                selectionStartX = contentArea.Origin.X;
                selectionWidth = contentArea.Size.X;
            }

            selectionStartX = Math.Clamp(selectionStartX, contentArea.Origin.X, contentArea.Origin.X + contentArea.Size.X);
            selectionWidth = Math.Clamp(selectionWidth, 0, contentArea.Origin.X + contentArea.Size.X - selectionStartX);

            if (selectionWidth > 0)
            {
                yield return DrawRectangle(
                    new(
                        new(selectionStartX, lineY),
                        new(selectionWidth, _lineHeight)
                    ),
                    new(0, 120, 215, 100),
                    NextDepth()
                );
            }
        }
    }

    private Rectangle<float> GetContentArea()
    {
        var bounds = Bounds;
        var x = bounds.Origin.X + BorderThickness + Padding;
        var y = bounds.Origin.Y + BorderThickness + Padding;
        var width = bounds.Size.X - BorderThickness * 2 - Padding * 2;
        var height = bounds.Size.Y - BorderThickness * 2 - Padding * 2;

        if (_lines.Count > _maxVisibleLines)
        {
            width -= ScrollbarWidth;
        }

        return new(new(x, y), new(Math.Max(0, width), Math.Max(0, height)));
    }

    private (int startLine, int startColumn, int endLine, int endColumn) GetNormalizedSelection()
    {
        if (_selectionStartLine < _selectionEndLine ||
            _selectionStartLine == _selectionEndLine && _selectionStartColumn <= _selectionEndColumn)
        {
            return (_selectionStartLine, _selectionStartColumn, _selectionEndLine, _selectionEndColumn);
        }

        return (_selectionEndLine, _selectionEndColumn, _selectionStartLine, _selectionStartColumn);
    }

    private (int line, int column) GetPositionFromMouse(Vector2 mousePosition)
    {
        var contentArea = GetContentArea();
        var relativeX = mousePosition.X - contentArea.Origin.X + _scrollOffsetX * _charWidth;
        var relativeY = mousePosition.Y - contentArea.Origin.Y + _scrollOffsetY * _lineHeight;

        var line = (int)Math.Floor(relativeY / _lineHeight);
        line = Math.Clamp(line, 0, _lines.Count - 1);

        var column = (int)Math.Floor(relativeX / _charWidth);
        column = Math.Clamp(column, 0, _lines[line].Length);

        return (line, column);
    }

    private string GetTextInRange(int startLine, int startColumn, int endLine, int endColumn)
    {
        if (startLine == endLine)
        {
            return _lines[startLine].Substring(startColumn, endColumn - startColumn);
        }

        var builder = new StringBuilder();

        builder.AppendLine(_lines[startLine].Substring(startColumn));

        for (var i = startLine + 1; i < endLine; i++)
        {
            builder.AppendLine(_lines[i]);
        }

        builder.Append(_lines[endLine].AsSpan(0, endColumn));

        return builder.ToString();
    }

    private int GetTotalTextLength()
    {
        return _lines.Sum(line => line.Length) + Math.Max(0, _lines.Count - 1);
    }

    private void InsertNewLine()
    {
        DeleteSelection();

        var currentLine = _lines[_cursorLine];
        var before = currentLine.Substring(0, _cursorColumn);
        var after = currentLine.Substring(_cursorColumn);

        _lines[_cursorLine] = before;
        _lines.Insert(_cursorLine + 1, after);

        SetCursorPosition(_cursorLine + 1, 0);
        UpdateTextFromLines();
    }

    private void MoveCursor(int deltaColumn, int deltaLine, bool extendSelection)
    {
        if (extendSelection)
        {
            if (!HasSelection)
            {
                StartSelection();
            }
        }
        else
        {
            ClearSelection();
        }

        var targetLine = Math.Clamp(_cursorLine + deltaLine, 0, _lines.Count - 1);
        var targetColumn = _cursorColumn;

        if (deltaColumn != 0)
        {
            targetColumn += deltaColumn;

            if (targetColumn < 0)
            {
                while (targetLine > 0 && targetColumn < 0)
                {
                    targetLine--;
                    targetColumn += _lines[targetLine].Length + 1;
                }

                targetColumn = Math.Max(0, targetColumn);
            }
            else
            {
                while (targetLine < _lines.Count - 1 && targetColumn > _lines[targetLine].Length)
                {
                    targetColumn -= _lines[targetLine].Length + 1;
                    targetLine++;
                }

                targetColumn = Math.Min(targetColumn, _lines[targetLine].Length);
            }
        }

        if (deltaLine != 0 && deltaColumn == 0)
        {
            targetColumn = Math.Min(_cursorColumn, _lines[targetLine].Length);
        }

        SetCursorPosition(targetLine, targetColumn);

        if (extendSelection)
        {
            UpdateSelection();
        }
    }

    private void ProcessKey(Key key, KeyboardState keyboardState)
    {
        var isShiftPressed = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
        var isControlPressed = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);

        switch (key)
        {
            case Key.Left:
                MoveCursor(-1, 0, isShiftPressed);

                break;
            case Key.Right:
                MoveCursor(1, 0, isShiftPressed);

                break;
            case Key.Up:
                MoveCursor(0, -1, isShiftPressed);

                break;
            case Key.Down:
                MoveCursor(0, 1, isShiftPressed);

                break;
            case Key.Home:
                if (isControlPressed)
                {
                    SetCursorPosition(0, 0);
                }
                else
                {
                    SetCursorPosition(_cursorLine, 0);
                }

                if (!isShiftPressed)
                {
                    ClearSelection();
                }

                break;
            case Key.End:
                if (isControlPressed)
                {
                    SetCursorPosition(_lines.Count - 1, _lines[^1].Length);
                }
                else
                {
                    SetCursorPosition(_cursorLine, _lines[_cursorLine].Length);
                }

                if (!isShiftPressed)
                {
                    ClearSelection();
                }

                break;
            case Key.PageUp:
                MoveCursor(0, -_maxVisibleLines, isShiftPressed);

                break;
            case Key.PageDown:
                MoveCursor(0, _maxVisibleLines, isShiftPressed);

                break;
            case Key.Enter:
                InsertNewLine();

                break;
            case Key.Backspace:
                Backspace();

                break;
            case Key.Delete:
                Delete();

                break;
            case Key.Tab:
                InsertText(new(' ', TabSize));

                break;
            case Key.A when isControlPressed:
                SelectAll();

                break;
            default:
                // Handle regular character input
                var shift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
                var character = KeyboardInputUtils.KeyToChar(key, shift);

                if (character.HasValue)
                {
                    InsertText(character.Value.ToString());
                }

                break;
        }
    }

    private void RecalculateLongestLine()
    {
        _longestLineLength = _lines.Count == 0 ? 0 : _lines.Max(line => line.Length);
    }

    private void SetCursorPosition(int line, int column)
    {
        line = Math.Clamp(line, 0, _lines.Count - 1);
        column = Math.Clamp(column, 0, _lines[line].Length);

        if (_cursorLine != line || _cursorColumn != column)
        {
            _cursorLine = line;
            _cursorColumn = column;
            CursorPositionChanged?.Invoke(this, EventArgs.Empty);
        }

        _cursorBlinkTimer = 0;
        _cursorVisible = true;

        // Auto-scroll to keep cursor visible
        UpdateScrolling();
    }

    private void SetTextInternal(string text)
    {
        _text = text;
        _lines.Clear();

        if (string.IsNullOrEmpty(text))
        {
            _lines.Add(string.Empty);
        }
        else
        {
            var normalized = text.Replace("\r\n", "\n");
            _lines.AddRange(normalized.Split('\n'));
        }

        if (_lines.Count == 0)
        {
            _lines.Add(string.Empty);
        }

        RecalculateLongestLine();
        _scrollOffsetX = 0;
        _scrollOffsetY = 0;
        SetCursorPosition(0, 0);
        ClearSelection();
        UpdateMeasurements();
    }

    private void StartSelection()
    {
        _selectionStartLine = _cursorLine;
        _selectionStartColumn = _cursorColumn;
        _selectionEndLine = _cursorLine;
        _selectionEndColumn = _cursorColumn;
        HasSelection = false;
    }

    private void UpdateMeasurements()
    {
        // Simple measurements - in real implementation would use font metrics
        _lineHeight = Theme.FontSize + 2;
        _charWidth = Math.Max(1f, Theme.FontSize * 0.6f); // Approximate char width

        var contentArea = GetContentArea();

        if (contentArea.Size.X > 0 && contentArea.Size.Y > 0)
        {
            _maxVisibleLines = Math.Max(1, (int)(contentArea.Size.Y / _lineHeight));
            _maxVisibleCharsPerLine = Math.Max(1, (int)(contentArea.Size.X / _charWidth));
        }
    }

    private void UpdateMouseSelection(Vector2 mousePosition)
    {
        var (line, column) = GetPositionFromMouse(mousePosition);
        SetCursorPosition(line, column);
        UpdateSelection();
    }

    private void UpdateScrolling()
    {
        if (_maxVisibleLines > 0)
        {
            if (_cursorLine < _scrollOffsetY)
            {
                _scrollOffsetY = _cursorLine;
            }
            else if (_cursorLine >= _scrollOffsetY + _maxVisibleLines)
            {
                _scrollOffsetY = _cursorLine - _maxVisibleLines + 1;
            }
        }

        if (_maxVisibleCharsPerLine > 0)
        {
            if (_cursorColumn < _scrollOffsetX)
            {
                _scrollOffsetX = _cursorColumn;
            }
            else if (_cursorColumn >= _scrollOffsetX + _maxVisibleCharsPerLine)
            {
                _scrollOffsetX = _cursorColumn - _maxVisibleCharsPerLine + 1;
            }
        }
    }

    private void UpdateSelection()
    {
        var previousHasSelection = HasSelection;

        _selectionEndLine = _cursorLine;
        _selectionEndColumn = _cursorColumn;

        var newHasSelection = !(_selectionStartLine == _selectionEndLine && _selectionStartColumn == _selectionEndColumn);
        HasSelection = newHasSelection;

        if (newHasSelection != previousHasSelection)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateTextFromLines()
    {
        var oldText = _text;
        _text = string.Join("\n", _lines);
        RecalculateLongestLine();
        UpdateMeasurements();
        TextChanged?.Invoke(this, new(oldText, _text));
    }

    private void UpdateTransformSize()
    {
        Transform.Size = new(_width, _height);
    }
}

/// <summary>
/// Event args for text changed event.
/// </summary>
public class TextChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the old text.
    /// </summary>
    public string OldText { get; }

    /// <summary>
    /// Gets the new text.
    /// </summary>
    public string NewText { get; }

    /// <summary>
    /// Initializes a new instance of TextChangedEventArgs.
    /// </summary>
    public TextChangedEventArgs(string oldText, string newText)
    {
        OldText = oldText;
        NewText = newText;
    }
}
