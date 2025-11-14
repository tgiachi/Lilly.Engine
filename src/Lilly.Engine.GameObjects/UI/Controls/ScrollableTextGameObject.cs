using System.Numerics;
using FontStashSharp;
using Lilly.Engine.Commands;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Serilog;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using TrippyGL;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// A scrollable text panel that displays long text content with mouse wheel scrolling support.
/// Features smooth scrolling, word wrapping, interactive scrollbar, and proper text clipping.
/// </summary>
public class ScrollableTextGameObject : BaseGameObject2D, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;
    private readonly ILogger _logger = Log.ForContext<ScrollableTextGameObject>();

    private DynamicSpriteFont? _font;
    private string _text = string.Empty;
    private readonly List<string> _lines = new();
    private float _lineHeight;

    // Scrolling state
    private float _scrollOffset;
    private float _maxScrollOffset;

    // Scrollbar dragging state
    private bool _isDraggingScrollbar;
    private float _dragStartY;
    private float _dragStartScrollOffset;

    // Configuration
    private float _width = 400f;
    private float _height = 300f;
    private int _padding = 10;
    private const int ScrollbarWidth = 10;
    private const int ScrollbarPadding = 4;

    /// <summary>
    /// Gets or sets the text content to display.
    /// </summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value ?? string.Empty;
                UpdateLines();
            }
        }
    }

    /// <summary>
    /// Gets or sets the width of the panel.
    /// </summary>
    public float Width
    {
        get => _width;
        set
        {
            if (Math.Abs(_width - value) > 0.001f)
            {
                _width = value;
                UpdateTransformSize();
                UpdateLines();
            }
        }
    }

    /// <summary>
    /// Gets or sets the height of the panel.
    /// </summary>
    public float Height
    {
        get => _height;
        set
        {
            if (Math.Abs(_height - value) > 0.001f)
            {
                _height = value;
                UpdateTransformSize();
                UpdateLines();
            }
        }
    }

    /// <summary>
    /// Gets or sets the padding inside the panel.
    /// </summary>
    public int Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = value;
                UpdateLines();
            }
        }
    }

    /// <summary>
    /// Gets or sets the scroll speed (pixels per mouse wheel notch).
    /// </summary>
    public float ScrollSpeed { get; set; } = 20f;

    /// <summary>
    /// Gets or sets whether the control is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the UI theme for styling.
    /// </summary>
    public UITheme Theme { get; }

    // Color properties with theme defaults
    public Color4b TextColor { get; set; }
    public Color4b BackgroundColor { get; set; }
    public Color4b BorderColor { get; set; }
    public Color4b ScrollBarTrackColor { get; set; }
    public Color4b ScrollBarThumbColor { get; set; }
    public Color4b ScrollBarThumbColorHovered { get; set; }
    public Color4b ScrollBarThumbColorDragged { get; set; }

    // IInputReceiver implementation
    public bool IsFocusable => true;
    public bool HasFocus { get; set; }

    public Rectangle<int> Bounds
        => new(
            (int)Transform.Position.X,
            (int)Transform.Position.Y,
            (int)_width,
            (int)_height
        );

    /// <summary>
    /// Initializes a new instance of the ScrollableTextGameObject class.
    /// </summary>
    /// <param name="inputManager">Input manager service.</param>
    /// <param name="assetManager">Asset manager service for loading fonts.</param>
    /// <param name="theme">UI theme for styling.</param>
    /// <param name="width">Width of the panel.</param>
    /// <param name="height">Height of the panel.</param>
    public ScrollableTextGameObject(
        IInputManagerService inputManager,
        IAssetManager assetManager,
        UITheme theme,
        float width = 400f,
        float height = 300f
    )
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));

        _width = width;
        _height = height;

        // Initialize colors from theme
        TextColor = theme.TextColor;
        BackgroundColor = theme.BackgroundColor;
        BorderColor = theme.BorderColor;
        ScrollBarTrackColor = theme.ScrollBarTrackColor;
        ScrollBarThumbColor = theme.ScrollBarThumbColor;
        ScrollBarThumbColorHovered = theme.ScrollBarThumbColorHovered;
        ScrollBarThumbColorDragged = theme.ScrollBarThumbColorDragged;

        Name = "ScrollableTextPanel";
        UpdateTransformSize();
        Initialize();
    }

    /// <summary>
    /// Gets the current scroll position as a percentage (0.0 to 1.0).
    /// </summary>
    public float GetScrollPercentage()
        => _maxScrollOffset > 0 ? _scrollOffset / _maxScrollOffset : 0f;

    // IInputReceiver implementation
    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus || !IsEnabled) { }

        // Optional: Add keyboard shortcuts for scrolling
        // Example: Arrow keys, Page Up/Down, Home/End
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsEnabled)
        {
            return;
        }

        var mousePos = new Vector2D<int>((int)mouseState.Position.X, (int)mouseState.Position.Y);

        // Handle scrollbar dragging
        if (_isDraggingScrollbar)
        {
            if (!_inputManager.IsMouseButtonDown(MouseButton.Left))
            {
                _isDraggingScrollbar = false;
            }
            else
            {
                var scrollbarBounds = GetScrollbarTrackBounds();
                var thumbHeight = GetScrollbarThumbHeight();
                var trackHeight = scrollbarBounds.Size.Y - thumbHeight;

                if (trackHeight > 0)
                {
                    var deltaY = mousePos.Y - _dragStartY;
                    var scrollDelta = deltaY / trackHeight * _maxScrollOffset;
                    _scrollOffset = Math.Clamp(_dragStartScrollOffset + scrollDelta, 0f, _maxScrollOffset);
                }
            }
        }
        else
        {
            // Check for scrollbar click to start dragging
            if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && _maxScrollOffset > 0)
            {
                var thumbBounds = GetScrollbarThumbBounds();

                if (IsPointInRectangle(mousePos, thumbBounds))
                {
                    _isDraggingScrollbar = true;
                    _dragStartY = mousePos.Y;
                    _dragStartScrollOffset = _scrollOffset;
                }
            }
        }
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        if (!IsEnabled || _font == null)
        {
            return;
        }

        var mousePos = new Vector2D<int>((int)mouseState.Position.X, (int)mouseState.Position.Y);

        // Only handle scrolling if mouse is over the control
        if (IsPointInRectangle(mousePos, Bounds))
        {
            var scrollWheels = mouseState.GetScrollWheels();

            if (scrollWheels.Length > 0)
            {
                var currentScrollValue = scrollWheels[0].Y;
                var previousScrollWheels = previousMouseState.GetScrollWheels();
                var previousScrollValue = previousScrollWheels.Length > 0 ? previousScrollWheels[0].Y : 0f;

                var scrollDelta = currentScrollValue - previousScrollValue;

                if (Math.Abs(scrollDelta) > 0.001f)
                {
                    _scrollOffset -= scrollDelta * ScrollSpeed;
                    _scrollOffset = Math.Clamp(_scrollOffset, 0f, _maxScrollOffset);
                }
            }
        }
    }

    public bool IsMouseInBounds(Vector2 mousePosition)
    {
        var mousePos = new Vector2D<int>((int)mousePosition.X, (int)mousePosition.Y);

        return IsPointInRectangle(mousePos, Bounds);
    }

    /// <summary>
    /// Scrolls by a specific amount.
    /// </summary>
    /// <param name="delta">Amount to scroll (positive = down, negative = up).</param>
    public void ScrollBy(float delta)
    {
        _scrollOffset = Math.Clamp(_scrollOffset + delta, 0f, _maxScrollOffset);
    }

    /// <summary>
    /// Scrolls to the bottom of the content.
    /// </summary>
    public void ScrollToBottom()
    {
        _scrollOffset = _maxScrollOffset;
    }

    /// <summary>
    /// Scrolls to the top of the content.
    /// </summary>
    public void ScrollToTop()
    {
        _scrollOffset = 0;
    }

    /// <summary>
    /// Sets the scroll position by percentage (0.0 to 1.0).
    /// </summary>
    public void SetScrollPercentage(float percentage)
    {
        _scrollOffset = Math.Clamp(percentage, 0f, 1f) * _maxScrollOffset;
    }

    /// <summary>
    /// Renders the scrollable text panel using RenderCommands.
    /// </summary>
    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible || _font == null)
        {
            yield break;
        }

        var bounds = new Rectangle<float>(
            Transform.Position,
            new(_width, _height)
        );

        // Draw background
        yield return DrawRectangle(bounds, BackgroundColor, NextDepth());

        // Draw border
        foreach (var cmd in DrawHollowRectangle(
                     Transform.Position,
                     new(_width, _height),
                     BorderColor,
                     Theme.BorderThickness,
                     NextDepth()
                 ))
        {
            yield return cmd;
        }

        // Draw scrollable text content with clipping
        foreach (var cmd in DrawScrollableContent(bounds))
        {
            yield return cmd;
        }

        // Draw scrollbar if needed
        if (_maxScrollOffset > 0)
        {
            foreach (var cmd in DrawScrollbar(bounds))
            {
                yield return cmd;
            }
        }
    }

    /// <summary>
    /// Breaks a long word into multiple chunks that fit within maxWidth.
    /// </summary>
    private List<string> BreakLongWord(string word, DynamicSpriteFont font, int maxWidth)
    {
        var result = new List<string>();
        var currentChunk = string.Empty;

        foreach (var ch in word)
        {
            var testChunk = currentChunk + ch;
            var testWidth = font.MeasureString(testChunk).X;

            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentChunk))
            {
                result.Add(currentChunk);
                currentChunk = ch.ToString();
            }
            else
            {
                currentChunk = testChunk;
            }
        }

        if (!string.IsNullOrEmpty(currentChunk))
        {
            result.Add(currentChunk);
        }

        return result.Count > 0 ? result : new() { word };
    }

    /// <summary>
    /// Draws the scrollable text content with smooth scrolling and proper clipping.
    /// </summary>
    private IEnumerable<RenderCommand> DrawScrollableContent(Rectangle<float> bounds)
    {
        if (_font == null || _lines.Count == 0)
        {
            yield break;
        }

        var visibleHeight = _height - _padding * 2;

        // Calculate visible range with sub-pixel scrolling
        var scrollOffsetInLines = _scrollOffset / _lineHeight;
        var startLine = (int)Math.Floor(scrollOffsetInLines);
        var startLineOffset = (scrollOffsetInLines - startLine) * _lineHeight;

        var maxVisibleLines = (int)Math.Ceiling(visibleHeight / _lineHeight) + 1;
        var endLine = Math.Min(startLine + maxVisibleLines, _lines.Count);

        // Create scissor rectangle for clipping
        var scissorRect = new Rectangle<int>(
            (int)(bounds.Origin.X + _padding),
            (int)(bounds.Origin.Y + _padding),
            (int)(_width - _padding * 2 - ScrollbarWidth - ScrollbarPadding * 2),
            (int)visibleHeight
        );

        // Begin scissor test
        yield return RenderCommandHelpers.CreateScissor(scissorRect);

        // Draw visible lines with sub-pixel positioning
        var startY = bounds.Origin.Y + _padding - startLineOffset;

        for (var i = startLine; i < endLine; i++)
        {
            var lineY = startY + (i - startLine) * _lineHeight;
            var position = new Vector2D<float>(bounds.Origin.X + _padding, lineY);

            yield return DrawTextCustom(
                Theme.FontName,
                _lines[i],
                Theme.FontSize,
                position,
                color: TextColor,
                depth: NextDepth()
            );
        }

        // End scissor test
        yield return RenderCommandHelpers.CreateDisableScissor();
    }

    /// <summary>
    /// Draws the interactive scrollbar.
    /// </summary>
    private IEnumerable<RenderCommand> DrawScrollbar(Rectangle<float> bounds)
    {
        var trackBounds = GetScrollbarTrackBounds();
        var thumbBounds = GetScrollbarThumbBounds();

        // Draw scrollbar track
        yield return DrawRectangle(trackBounds, ScrollBarTrackColor, NextDepth());

        // Determine thumb color based on state
        var thumbColor = _isDraggingScrollbar ? ScrollBarThumbColorDragged :
                         IsMouseOverScrollbarThumb() ? ScrollBarThumbColorHovered : ScrollBarThumbColor;

        // Draw scrollbar thumb
        yield return DrawRectangle(thumbBounds, thumbColor, NextDepth());

        // Draw thumb border for better visibility
        foreach (var cmd in DrawHollowRectangle(
                     thumbBounds.Origin,
                     thumbBounds.Size,
                     BorderColor,
                     1,
                     NextDepth()
                 ))
        {
            yield return cmd;
        }
    }

    /// <summary>
    /// Gets the scrollbar thumb bounds.
    /// </summary>
    private Rectangle<float> GetScrollbarThumbBounds()
    {
        var trackBounds = GetScrollbarTrackBounds();
        var thumbHeight = GetScrollbarThumbHeight();
        var trackHeight = trackBounds.Size.Y - thumbHeight;

        var thumbPosition = _maxScrollOffset > 0
                                ? trackBounds.Origin.Y + trackHeight * (_scrollOffset / _maxScrollOffset)
                                : trackBounds.Origin.Y;

        return new(
            new(trackBounds.Origin.X, thumbPosition),
            new(ScrollbarWidth, thumbHeight)
        );
    }

    /// <summary>
    /// Gets the scrollbar thumb height.
    /// </summary>
    private float GetScrollbarThumbHeight()
    {
        var visibleHeight = _height - _padding * 2;
        var contentHeight = _lines.Count * _lineHeight;
        var trackBounds = GetScrollbarTrackBounds();

        return Math.Max(20, trackBounds.Size.Y * (visibleHeight / contentHeight));
    }

    /// <summary>
    /// Gets the scrollbar track bounds.
    /// </summary>
    private Rectangle<float> GetScrollbarTrackBounds()
        => new(
            new(
                Transform.Position.X + _width - ScrollbarWidth - ScrollbarPadding,
                Transform.Position.Y + ScrollbarPadding
            ),
            new(
                ScrollbarWidth,
                _height - ScrollbarPadding * 2
            )
        );

    /// <summary>
    /// Initializes the scrollable text panel.
    /// </summary>
    private void Initialize()
    {
        try
        {
            _font = _assetManager.GetFont<DynamicSpriteFont>(Theme.FontName, Theme.FontSize);

            if (_font != null)
            {
                _lineHeight = _font.MeasureString("Ay").Y;
                UpdateLines();
            }
            else
            {
                _logger.Warning(
                    "Font '{FontName}' with size {FontSize} could not be loaded",
                    Theme.FontName,
                    Theme.FontSize
                );
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to initialize ScrollableTextGameObject: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Checks if the mouse is currently over the scrollbar thumb.
    /// </summary>
    private bool IsMouseOverScrollbarThumb()
    {
        if (_maxScrollOffset <= 0 || _inputManager.CurrentMouseState is null)
        {
            return false;
        }

        var mouseState = _inputManager.CurrentMouseState;
        var mousePos = new Vector2D<int>((int)mouseState.Position.X, (int)mouseState.Position.Y);
        var thumbBounds = GetScrollbarThumbBounds();

        return IsPointInRectangle(mousePos, thumbBounds);
    }

    private bool IsPointInRectangle(Vector2D<int> point, Rectangle<int> rect)
        => point.X >= rect.Origin.X &&
           point.X < rect.Origin.X + rect.Size.X &&
           point.Y >= rect.Origin.Y &&
           point.Y < rect.Origin.Y + rect.Size.Y;

    private bool IsPointInRectangle(Vector2D<int> point, Rectangle<float> rect)
        => point.X >= rect.Origin.X &&
           point.X < rect.Origin.X + rect.Size.X &&
           point.Y >= rect.Origin.Y &&
           point.Y < rect.Origin.Y + rect.Size.Y;

    /// <summary>
    /// Updates the line list based on current text and panel width with improved word wrapping.
    /// </summary>
    private void UpdateLines()
    {
        _lines.Clear();

        if (_font == null || string.IsNullOrEmpty(_text))
        {
            _maxScrollOffset = 0;
            _scrollOffset = 0;

            return;
        }

        var maxLineWidth = (int)(_width - _padding * 2 - ScrollbarWidth - ScrollbarPadding * 2);
        var textLines = _text.Split('\n');

        // Wrap each line if needed
        foreach (var line in textLines)
        {
            var wrappedLines = WrapTextToLines(line, _font, maxLineWidth);
            _lines.AddRange(wrappedLines);
        }

        // Update max scroll offset
        var visibleHeight = _height - _padding * 2;
        var contentHeight = _lines.Count * _lineHeight;
        var maxVisibleLines = visibleHeight / _lineHeight;

        _maxScrollOffset = Math.Max(0, contentHeight - visibleHeight);

        // Clamp current scroll offset
        _scrollOffset = Math.Clamp(_scrollOffset, 0f, _maxScrollOffset);
    }

    private void UpdateTransformSize()
    {
        Transform.Size = new(_width, _height);
    }

    /// <summary>
    /// Wraps a single line of text into multiple lines, handling long words properly.
    /// </summary>
    private List<string> WrapTextToLines(string text, DynamicSpriteFont font, int maxWidth)
    {
        var result = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            result.Add(string.Empty);

            return result;
        }

        // Handle case where maxWidth is too small
        if (maxWidth < font.MeasureString("M").X)
        {
            result.Add(text);

            return result;
        }

        var words = text.Split(' ');
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            // Handle extremely long words that exceed maxWidth
            if (font.MeasureString(word).X > maxWidth)
            {
                // Flush current line if not empty
                if (!string.IsNullOrEmpty(currentLine))
                {
                    result.Add(currentLine);
                    currentLine = string.Empty;
                }

                // Break the long word into chunks
                var brokenWord = BreakLongWord(word, font, maxWidth);
                result.AddRange(brokenWord.SkipLast(1)); // Add all but the last chunk
                currentLine = brokenWord.Last();         // Keep last chunk for potential continuation

                continue;
            }

            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testWidth = font.MeasureString(testLine).X;

            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                result.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            result.Add(currentLine);
        }

        if (result.Count == 0)
        {
            result.Add(string.Empty);
        }

        return result;
    }
}
