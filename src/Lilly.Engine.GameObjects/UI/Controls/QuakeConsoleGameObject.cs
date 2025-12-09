using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.GameObjects.UI.Controls.Console;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Input;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Utils;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// Provides a Quake-style drop-down console for command entry and history browsing.
/// </summary>
public sealed class QuakeConsoleGameObject : Base2dGameObject, IInputReceiver, IDisposable
{
    private const int ConsoleHeight = 320;
    private const float DefaultAnimationSpeed = 700f;
    private const float DefaultLineSpacing = 4f;

    private readonly List<ConsoleEntry> _entries = [];
    private readonly List<string> _history = [];
    private readonly List<string> _autoCompleteSuggestions = [];
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private bool _isAnimating;
    private bool _isInitialized;
    private bool _isOpen;
    private bool _hasFocus;
    private readonly string _fontFamily;
    private readonly int _fontSize = 16;
    private string _inputBuffer = string.Empty;
    private KeyboardState _previousKeyboardState;
    private int _historyIndex = -1;
    private string _historyOriginal = string.Empty;
    private int _autoCompleteIndex = -1;
    private float _currentY = -ConsoleHeight;
    private float _targetY = -ConsoleHeight;
    private float _lineHeight = 16f;
    private int _maxLines = 1;
    private double _lastBlinkTime;
    private bool _cursorVisible = true;
    private int _viewportWidth = 1280;
    private int _viewportHeight = 720;

    /// <summary>
    /// Occurs when the user submits a command.
    /// </summary>
    public event EventHandler<string>? CommandSubmitted;

    /// <summary>
    /// Gets or sets the animation speed in pixels per second.
    /// </summary>
    public float AnimationSpeed { get; set; } = DefaultAnimationSpeed;

    /// <summary>
    /// Gets or sets the console background colour.
    /// </summary>
    public Color4b BackgroundColor { get; set; } = new(0, 0, 0, 200);

    /// <summary>
    /// Gets or sets the default foreground colour.
    /// </summary>
    public Color4b ForegroundColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets a modifiable list of welcome lines shown at initialisation.
    /// </summary>
    public List<string> WelcomeLines { get; } = [];

    /// <summary>
    /// Gets or sets the command prompt prefix.
    /// </summary>
    public string Prompt { get; set; } = "> ";

    /// <summary>
    /// Gets or sets the caret blink interval in seconds.
    /// </summary>
    public float CaretBlinkInterval { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the delegate for auto-complete suggestions.
    /// </summary>
    public Func<string, IEnumerable<string>>? GetAutoCompleteSuggestions { get; set; }

    /// <summary>
    /// Gets or sets whether this console can receive focus.
    /// </summary>
    public bool IsFocusable => true;

    /// <summary>
    /// Gets or sets whether this console has input focus.
    /// </summary>
    public bool HasFocus
    {
        get => _hasFocus;
        set
        {
            if (_hasFocus != value)
            {
                _hasFocus = value;

                if (!_hasFocus)
                {
                    _cursorVisible = false;
                }
            }
        }
    }

    /// <summary>
    /// Gets the bounds of the console.
    /// </summary>
    public Vector2 Bounds => new(_viewportWidth, ConsoleHeight);

    /// <summary>
    /// Initializes a new instance of the QuakeConsoleGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    /// <param name="assetManager">The asset manager service.</param>
    /// <param name="renderContext">The render context.</param>
    /// <param name="uiTheme">The UI theme.</param>
    /// <param name="gameObjectManager">The game object manager.</param>
    public QuakeConsoleGameObject(
        IInputManagerService inputManager,
        IAssetManager assetManager,
        RenderContext renderContext,
        UITheme uiTheme,
        IRenderPipeline gameObjectManager
    ) : base("QuakeConsole", gameObjectManager, 9999)
    {
        _inputManager = inputManager;
        _assetManager = assetManager;
        _fontFamily = uiTheme.FontName;
        _fontSize = uiTheme.FontSize;
        Initialize((int)renderContext.GraphicsDevice.Viewport.Width, (int)renderContext.GraphicsDevice.Viewport.Height);
    }

    /// <summary>
    /// Adds a console line using the default colours.
    /// </summary>
    /// <param name="text">The line text.</param>
    public void AddLine(string text)
    {
        AddLine(text, ForegroundColor);
    }

    /// <summary>
    /// Adds a console line using a custom foreground colour.
    /// </summary>
    /// <param name="text">The line text.</param>
    /// <param name="foreground">The foreground colour.</param>
    public void AddLine(string text, Color4b foreground)
    {
        AddLine(text, foreground, null);
    }

    /// <summary>
    /// Adds a console line using custom colours.
    /// </summary>
    /// <param name="text">The line text.</param>
    /// <param name="foreground">The foreground colour.</param>
    /// <param name="background">The optional background colour.</param>
    public void AddLine(string text, Color4b foreground, Color4b? background)
    {
        if (_entries.Count >= _maxLines)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(new(text, foreground, background));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseFocus();
        _entries.Clear();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!_isInitialized || !HasFocus)
        {
            return;
        }

        var justPressed = GetJustPressedKeys(keyboardState, _previousKeyboardState);

        foreach (var key in justPressed)
        {
            if (key == Key.Enter)
            {
                SubmitCommand();
            }
            else if (key == Key.Backspace && _inputBuffer.Length > 0)
            {
                _inputBuffer = _inputBuffer[..^1];
                ResetHistoryNavigation();
                ResetAutoComplete();
            }
            else if (key == Key.Up)
            {
                TraverseHistory(true);
            }
            else if (key == Key.Down)
            {
                TraverseHistory(false);
            }
            else if (key == Key.Tab)
            {
                HandleAutoComplete();
            }
            else
            {
                var shift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
                var value = KeyboardInputUtils.KeyToString(key, shift);

                if (!string.IsNullOrEmpty(value))
                {
                    _inputBuffer += value;
                    ResetHistoryNavigation();
                    ResetAutoComplete();
                }
            }
        }

        _previousKeyboardState = keyboardState;
    }

    /// <inheritdoc />
    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        // Not implemented
    }

    /// <inheritdoc />
    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented
    }

    /// <summary>
    /// Hides the console.
    /// </summary>
    public void Hide()
    {
        if (!_isOpen || _isAnimating)
        {
            return;
        }

        _targetY = -ConsoleHeight;
        _isOpen = false;
        _isAnimating = true;
        ReleaseFocus();
    }

    /// <inheritdoc />
    public bool IsMouseInBounds(Vector2 mousePosition)
    {
        var boundsX = (int)mousePosition.X >= 0 && (int)mousePosition.X <= _viewportWidth;
        var boundsY = (int)mousePosition.Y >= (int)_currentY && (int)mousePosition.Y <= (int)(_currentY + ConsoleHeight);

        return boundsX && boundsY;
    }

    /// <summary>
    /// Shows the console.
    /// </summary>
    public void Show()
    {
        if (_isOpen || _isAnimating)
        {
            return;
        }

        CaptureFocus();
        _targetY = 0f;
        _isOpen = true;
        _isAnimating = true;
        IsActive = true;
    }

    /// <summary>
    /// Toggles the console visibility.
    /// </summary>
    public void ToggleConsole()
    {
        if (_isAnimating)
        {
            return;
        }

        if (_isOpen)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    /// <inheritdoc />
    public override void Update(GameTime gameTime)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (_isAnimating)
        {
            var delta = gameTime.GetElapsedSeconds();
            var step = AnimationSpeed * delta;

            if (_currentY < _targetY)
            {
                _currentY = Math.Min(_currentY + step, _targetY);
            }
            else if (_currentY > _targetY)
            {
                _currentY = Math.Max(_currentY - step, _targetY);
            }

            Transform.Position = new(0f, _currentY);

            if (Math.Abs(_currentY - _targetY) < 0.5f)
            {
                _currentY = _targetY;
                Transform.Position = new(0f, _currentY);
                _isAnimating = false;

                if (!_isOpen)
                {
                    IsActive = false;
                }
            }
        }
    }

    /// <inheritdoc />
    protected override void OnDraw(GameTime gameTime)
    {
        if (!_isInitialized || !IsActive || SpriteBatcher == null)
        {
            return;
        }

        // Background
        var backgroundPos = new Vector2(0, _currentY);
        var backgroundSize = new Vector2(_viewportWidth, ConsoleHeight);
        SpriteBatcher.DrawRectangle(backgroundPos, backgroundSize, BackgroundColor);

        // Console entries
        var lineY = _currentY + 10;

        for (var i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            var textPosition = new Vector2(10f, lineY);

            if (entry.Background.HasValue)
            {
                var textWidth = TextMeasurement.MeasureStringWidth(_assetManager, entry.Text, _fontFamily, _fontSize);
                var rectPos = new Vector2(textPosition.X - 2, textPosition.Y - 2);
                var rectSize = new Vector2(textWidth + 4, _fontSize + 4);
                SpriteBatcher.DrawRectangle(rectPos, rectSize, entry.Background.Value);
            }

            SpriteBatcher.DrawText(
                _fontFamily,
                _fontSize,
                entry.Text,
                textPosition,
                entry.Foreground
            );

            lineY += _lineHeight;
        }

        // Input line
        var inputText = Prompt + _inputBuffer;
        var inputPosition = new Vector2(10f, _currentY + ConsoleHeight - 25f);

        SpriteBatcher.DrawText(
            _fontFamily,
            _fontSize,
            inputText,
            inputPosition,
            ForegroundColor
        );

        // Cursor
        if (ShouldShowCaret(gameTime))
        {
            var textWidth = TextMeasurement.MeasureStringWidth(_assetManager, inputText, _fontFamily, _fontSize);
            var caretPosition = new Vector2(inputPosition.X + textWidth, inputPosition.Y);

            SpriteBatcher.DrawText(
                _fontFamily,
                _fontSize,
                "_",
                caretPosition,
                ForegroundColor
            );
        }
    }

    private void AppendHistory(string command)
    {
        _history.Remove(command);
        _history.Add(command);

        if (_history.Count > 100)
        {
            _history.RemoveAt(0);
        }
    }

    private void CaptureFocus()
    {
        if (!_hasFocus)
        {
            _inputManager.SetFocus(this);
            _hasFocus = true;
        }
    }

    private static List<Key> GetJustPressedKeys(KeyboardState current, KeyboardState? previous)
    {
        if (previous == null)
        {
            return [];
        }

        var pressed = current.GetPressedKeys();
        var list = new List<Key>(pressed.Length);

        for (var i = 0; i < pressed.Length; i++)
        {
            var key = pressed[i];

            if (!previous.IsKeyPressed(key))
            {
                list.Add(key);
            }
        }

        return list;
    }

    private void HandleAutoComplete()
    {
        if (GetAutoCompleteSuggestions == null)
        {
            return;
        }

        if (_autoCompleteIndex == -1)
        {
            _autoCompleteSuggestions.Clear();
            _autoCompleteSuggestions.AddRange(GetAutoCompleteSuggestions(_inputBuffer));

            if (_autoCompleteSuggestions.Count == 0)
            {
                return;
            }
            _autoCompleteIndex = 0;
        }
        else
        {
            _autoCompleteIndex = (_autoCompleteIndex + 1) % _autoCompleteSuggestions.Count;
        }

        _inputBuffer = _autoCompleteSuggestions[_autoCompleteIndex];
        ResetHistoryNavigation();
    }

    private bool HandleInternalCommand(string command)
    {
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return false;
        }

        var keyword = parts[0].ToLowerInvariant();
        var handled = true;

        switch (keyword)
        {
            case "help":
                ShowHelp();

                break;
            case "clear":
                _entries.Clear();

                break;
            case "exit":
            case "close":
                Hide();

                break;
            case "show":
                Show();

                break;
            case "hide":
                Hide();

                break;
            case "echo":
                var echo = parts.Length > 1 ? string.Join(" ", parts[1..]) : string.Empty;
                AddLine(string.IsNullOrWhiteSpace(echo) ? "echo: missing arguments" : echo, ForegroundColor);

                break;
            case "history":
                ShowHistory();

                break;
            default:
                handled = false;

                break;
        }

        if (handled)
        {
            ResetHistoryNavigation();
        }

        return handled;
    }

    /// <summary>
    /// Initializes the console resources.
    /// </summary>
    /// <param name="viewportWidth">The viewport width.</param>
    /// <param name="viewportHeight">The viewport height.</param>
    private void Initialize(int viewportWidth, int viewportHeight)
    {
        if (_isInitialized)
        {
            return;
        }

        _viewportWidth = viewportWidth;
        _viewportHeight = viewportHeight;

        _lineHeight = _fontSize + DefaultLineSpacing;

        RecalculateLayout();

        foreach (var line in WelcomeLines)
        {
            AddLine(line, new(144, 238, 144));
        }

        IsActive = false;
        _isInitialized = true;
    }

    private void RecalculateLayout()
    {
        Transform.Size = new(_viewportWidth, ConsoleHeight);
        Transform.Position = new(0f, _currentY);
        _maxLines = (int)Math.Max(1, (ConsoleHeight - 40f) / _lineHeight);
        _entries.Clear();
    }

    private void ReleaseFocus()
    {
        if (_hasFocus)
        {
            _inputManager.ClearFocus();
            _hasFocus = false;
        }
    }

    private void ResetAutoComplete()
    {
        _autoCompleteIndex = -1;
        _autoCompleteSuggestions.Clear();
    }

    private void ResetHistoryNavigation()
    {
        _historyIndex = -1;
        _historyOriginal = string.Empty;
    }

    private bool ShouldShowCaret(GameTime gameTime)
    {
        if (!HasFocus || CaretBlinkInterval <= 0f)
        {
            return false;
        }

        var currentTime = gameTime.GetTotalGameTimeSeconds();
        var elapsed = currentTime - _lastBlinkTime;

        if (elapsed >= CaretBlinkInterval)
        {
            _cursorVisible = !_cursorVisible;
            _lastBlinkTime = currentTime;
        }

        return _cursorVisible;
    }

    private void ShowHelp()
    {
        AddLine("Available commands:", new(0, 255, 255));
        AddLine("  help      - Show this help message", ForegroundColor);
        AddLine("  clear     - Clear console output", ForegroundColor);
        AddLine("  history   - Show recent commands", ForegroundColor);
        AddLine("  echo text - Echo text to the console", ForegroundColor);
        AddLine("  show      - Open the console", ForegroundColor);
        AddLine("  hide      - Close the console", ForegroundColor);
        AddLine("  exit      - Close the console", ForegroundColor);
    }

    private void ShowHistory()
    {
        if (_history.Count == 0)
        {
            AddLine("No history entries yet.", new(128, 128, 128));

            return;
        }

        AddLine("Recent commands:", new(0, 255, 255));
        var count = Math.Min(10, _history.Count);

        for (var i = _history.Count - count; i < _history.Count; i++)
        {
            AddLine($"  {_history[i]}", ForegroundColor);
        }
    }

    private void SubmitCommand()
    {
        var command = _inputBuffer.Trim();
        AddLine(Prompt + command, ForegroundColor);

        if (!string.IsNullOrEmpty(command))
        {
            AppendHistory(command);
            CommandSubmitted?.Invoke(this, command);

            if (HandleInternalCommand(command))
            {
                _inputBuffer = string.Empty;

                return;
            }
        }

        _inputBuffer = string.Empty;
        ResetHistoryNavigation();
        ResetAutoComplete();
    }

    private void TraverseHistory(bool up)
    {
        if (_history.Count == 0)
        {
            return;
        }

        if (_historyIndex == -1)
        {
            _historyOriginal = _inputBuffer;
        }

        if (up)
        {
            _historyIndex = _historyIndex <= 0 ? _history.Count - 1 : _historyIndex - 1;
        }
        else if (_historyIndex == -1)
        {
            return;
        }
        else
        {
            _historyIndex++;

            if (_historyIndex >= _history.Count)
            {
                _historyIndex = -1;
                _inputBuffer = _historyOriginal;

                return;
            }
        }

        if (_historyIndex >= 0 && _historyIndex < _history.Count)
        {
            _inputBuffer = _history[_historyIndex];
        }
    }
}
