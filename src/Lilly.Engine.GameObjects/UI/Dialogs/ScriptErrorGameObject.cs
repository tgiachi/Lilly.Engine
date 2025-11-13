using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Data.Scripts;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.GameObjects.Utils;
using Lilly.Engine.Commands;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Utils;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using TrippyGL;
using MouseButton = Lilly.Engine.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Dialogs;

/// <summary>
/// A modal dialog that displays script error information.
/// This component shows detailed error information including message, file location, line number, and stack trace.
/// It's typically displayed as a full-screen overlay when a script error occurs.
/// </summary>
public class ScriptErrorGameObject : BaseGameObject2D, IInputReceiver
{
    private ScriptErrorInfo? _errorInfo;
    private readonly IAssetManager _assetManager;
    private readonly IInputManagerService _inputManager;
    private readonly UITheme _theme;
    private bool _isInitialized;
    private Rectangle<int> _closeButtonBounds;
    private bool _isCloseButtonHovered;

    // Dialog dimensions and styling
    private const int DialogWidth = 700;
    private const int DialogMaxHeight = 500;
    private const int DialogPadding = 20;
    private const int TitleBarHeight = 40;
    private const int ButtonHeight = 35;
    private const int ButtonWidth = 100;
    private const int Spacing = 10;
    private const int LineHeight = 16;
    private const int TitleFontSize = 18;
    private const int ContentFontSize = 14;

    // Scrolling for stack trace
    private float _stackTraceScrollOffset;
    private float _stackTraceMaxScroll;

    private Viewport _viewportSize;

    /// <summary>
    /// Gets or sets the error information to display.
    /// Setting this property automatically shows the dialog.
    /// </summary>
    public ScriptErrorInfo? ErrorInfo
    {
        get => _errorInfo;
        set
        {
            _errorInfo = value;

            if (value != null)
            {
                IsVisible = true;
                _stackTraceScrollOffset = 0; // Reset scroll position
            }
        }
    }

    /// <summary>
    /// Event fired when the dialog is closed.
    /// </summary>
    public event EventHandler? Closed;

    /// <summary>
    /// Initializes a new instance of the ScriptErrorGameObject class.
    /// </summary>
    /// <param name="errorInfo">The error information to display.</param>
    /// <param name="assetManager">The asset manager service for loading fonts.</param>
    /// <param name="inputManager">The input manager service for handling input.</param>
    /// <param name="theme">The UI theme for styling the dialog.</param>
    public ScriptErrorGameObject(
        IAssetManager assetManager,
        RenderContext renderContext,
        IInputManagerService inputManager,
        IScriptEngineService scriptEngineService,
        UITheme theme
    )
    {
        ArgumentNullException.ThrowIfNull(assetManager);
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(theme);

        _viewportSize = renderContext.GraphicsDevice.Viewport;

        _assetManager = assetManager;
        _inputManager = inputManager;
        _theme = theme;
        scriptEngineService.OnScriptError += ScriptEngineServiceOnOnScriptError;

        Name = "ScriptErrorDialog";
        Order = 1000;      // Ensure it's rendered on top of everything
        IsVisible = false; // Hidden until ErrorInfo is set

        // Full screen size will be set during update
        Transform.Position = Vector2D<float>.Zero;
    }

    private void ScriptEngineServiceOnOnScriptError(object? sender, ScriptErrorInfo e)
    {
        ErrorInfo = e;
    }

    /// <summary>
    /// Closes the dialog and fires the Closed event.
    /// </summary>
    public void Close()
    {
        Closed?.Invoke(this, EventArgs.Empty);
        IsVisible = false;
    }

    public bool IsFocusable => true;

    public bool HasFocus { get; set; }

    public Rectangle<int> Bounds
        => new(
            new Vector2D<int>((int)Transform.Position.X, (int)Transform.Position.Y),
            new Vector2D<int>((int)Transform.Size.X, (int)Transform.Size.Y)
        );

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!IsVisible)
        {
            return;
        }

        // Handle Escape to close
        if (keyboardState.IsKeyPressed(Key.Escape) && !previousKeyboardState.IsKeyPressed(Key.Escape))
        {
            Close();
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        if (!IsVisible)
        {
            return;
        }

        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);

        // Check if close button is hovered
        _isCloseButtonHovered = _closeButtonBounds.Contains(new Vector2D<int>((int)mousePos.X, (int)mousePos.Y));

        // Check if close button is clicked
        if (_isCloseButtonHovered && _inputManager.IsMouseButtonPressed(MouseButton.Left))
        {
            Close();
        }
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        if (!IsVisible || ErrorInfo == null)
        {
            return;
        }

        // Handle scroll wheel for stack trace
        var scrollWheels = mouseState.GetScrollWheels();

        if (scrollWheels.Length > 0)
        {
            var currentScrollValue = scrollWheels[0].Y;
            var previousScrollWheels = previousMouseState.GetScrollWheels();
            var previousScrollValue = previousScrollWheels.Length > 0 ? previousScrollWheels[0].Y : 0f;

            var scrollDelta = (currentScrollValue - previousScrollValue) * 20f; // Scale factor for smooth scrolling

            _stackTraceScrollOffset -= scrollDelta;
            _stackTraceScrollOffset = Math.Clamp(_stackTraceScrollOffset, 0, _stackTraceMaxScroll);
        }
    }

    public bool IsMouseInBounds(Vector2 mousePosition)
    {
        return Bounds.Contains(new Vector2D<int>((int)mousePosition.X, (int)mousePosition.Y));
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsVisible)
        {
            return;
        }

        if (!_isInitialized)
        {
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Renders the error dialog.
    /// </summary>
    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        var viewport = _viewportSize;

        // Draw full-screen overlay
        foreach (var cmd in DrawOverlay(viewport))
        {
            yield return cmd;
        }

        // Calculate dialog position (centered)
        var dialogX = (viewport.X - DialogWidth) / 2;
        var dialogY = 50; // Some padding from top

        // Calculate content height
        var contentHeight = CalculateContentHeight();
        var dialogHeight = Math.Min(contentHeight + TitleBarHeight + ButtonHeight + DialogPadding * 3, DialogMaxHeight);

        var dialogBounds = new Rectangle<float>(dialogX, dialogY, DialogWidth, dialogHeight);

        // Draw dialog background
        foreach (var cmd in DrawDialogBackground(dialogBounds))
        {
            yield return cmd;
        }

        // Draw title bar
        foreach (var cmd in DrawTitleBar(dialogBounds))
        {
            yield return cmd;
        }

        // Draw error content
        foreach (var cmd in DrawErrorContent(dialogBounds))
        {
            yield return cmd;
        }

        // Calculate and draw close button
        _closeButtonBounds = new Rectangle<int>(
            (int)(dialogBounds.Origin.X + dialogBounds.Size.X - ButtonWidth - DialogPadding),
            (int)(dialogBounds.Origin.Y + dialogBounds.Size.Y - ButtonHeight - DialogPadding),
            ButtonWidth,
            ButtonHeight
        );

        foreach (var cmd in DrawCloseButton())
        {
            yield return cmd;
        }
    }

    /// <summary>
    /// Calculates the total height needed for the dialog content.
    /// </summary>
    private int CalculateContentHeight()
    {
        var height = 0;
        var maxWidth = DialogWidth - DialogPadding * 2;

        if (!string.IsNullOrEmpty(ErrorInfo.ErrorType))
        {
            var size = TextMeasurement.MeasureString(_assetManager, ErrorInfo.ErrorType, _theme.FontName, ContentFontSize);
            height += (int)size.Y + Spacing;
        }

        if (!string.IsNullOrEmpty(ErrorInfo.Message))
        {
            var wrappedMessage = WrapText(ErrorInfo.Message, maxWidth);
            var size = TextMeasurement.MeasureString(_assetManager, wrappedMessage, _theme.FontName, ContentFontSize);
            height += (int)size.Y + Spacing * 2;
        }

        if (!string.IsNullOrEmpty(ErrorInfo.FileName) || ErrorInfo.LineNumber.HasValue)
        {
            var size = TextMeasurement.MeasureString(_assetManager, "File:", _theme.FontName, ContentFontSize);
            height += (int)size.Y + Spacing * 2;
        }

        if (!string.IsNullOrEmpty(ErrorInfo.StackTrace))
        {
            var size = TextMeasurement.MeasureString(_assetManager, "Stack Trace:", _theme.FontName, ContentFontSize);
            height += (int)size.Y + Spacing;
            height += 150; // Fixed height for stack trace box
        }

        return height;
    }

    /// <summary>
    /// Draws the semi-transparent overlay.
    /// </summary>
    private IEnumerable<RenderCommand> DrawOverlay(Viewport viewport)
    {
        var overlayBounds = new Rectangle<float>(0, 0, viewport.Width, viewport.Height);

        yield return DrawRectangle(overlayBounds, _theme.ScriptErrorOverlayColor, depth: NextDepth());
    }

    /// <summary>
    /// Draws the dialog background.
    /// </summary>
    private IEnumerable<RenderCommand> DrawDialogBackground(Rectangle<float> bounds)
    {
        yield return DrawRectangle(bounds, _theme.ScriptErrorDialogBackgroundColor, depth: NextDepth());

        // Draw border
        foreach (var cmd in DrawBorder(bounds, _theme.ScriptErrorCodeBorderColor, 2f, depth: NextDepth()))
        {
            yield return cmd;
        }
    }

    /// <summary>
    /// Draws the title bar.
    /// </summary>
    private IEnumerable<RenderCommand> DrawTitleBar(Rectangle<float> dialogBounds)
    {
        var titleBarBounds = new Rectangle<float>(
            dialogBounds.Origin.X,
            dialogBounds.Origin.Y,
            dialogBounds.Size.X,
            TitleBarHeight
        );

        yield return DrawRectangle(titleBarBounds, _theme.ScriptErrorTitleBarColor, depth: NextDepth());

        var title = "Script Error";
        var titleSize = TextMeasurement.MeasureString(_assetManager, title, _theme.FontName, TitleFontSize);
        var titlePosition = new Vector2D<float>(
            titleBarBounds.Origin.X + DialogPadding,
            titleBarBounds.Origin.Y + (TitleBarHeight - titleSize.Y) / 2f
        );

        yield return DrawTextCustom(
            _theme.FontName,
            title,
            TitleFontSize,
            titlePosition,
            color: _theme.ScriptErrorTitleTextColor,
            depth: NextDepth()
        );
    }

    /// <summary>
    /// Draws the error content.
    /// </summary>
    private IEnumerable<RenderCommand> DrawErrorContent(Rectangle<float> dialogBounds)
    {
        var contentX = dialogBounds.Origin.X + DialogPadding;
        var currentY = dialogBounds.Origin.Y + TitleBarHeight + DialogPadding;
        var maxWidth = DialogWidth - DialogPadding * 2;

        // Error Type and Message
        if (!string.IsNullOrEmpty(ErrorInfo.ErrorType))
        {
            var errorTypeText = $"{ErrorInfo.ErrorType}";
            var textPos = new Vector2D<float>(contentX, currentY);

            yield return DrawTextCustom(
                _theme.FontName,
                errorTypeText,
                ContentFontSize,
                textPos,
                color: _theme.ScriptErrorTextColor,
                depth: NextDepth()
            );

            var size = TextMeasurement.MeasureString(_assetManager, errorTypeText, _theme.FontName, ContentFontSize);
            currentY += size.Y + Spacing;
        }

        if (!string.IsNullOrEmpty(ErrorInfo.Message))
        {
            var wrappedMessage = WrapText(ErrorInfo.Message, maxWidth);
            var textPos = new Vector2D<float>(contentX, currentY);

            yield return DrawTextCustom(
                _theme.FontName,
                wrappedMessage,
                ContentFontSize,
                textPos,
                color: _theme.ScriptErrorLabelColor,
                depth: NextDepth()
            );

            var size = TextMeasurement.MeasureString(_assetManager, wrappedMessage, _theme.FontName, ContentFontSize);
            currentY += size.Y + Spacing * 2;
        }

        // File and Line information
        if (!string.IsNullOrEmpty(ErrorInfo.FileName) || ErrorInfo.LineNumber.HasValue)
        {
            var locationText = $"File: {ErrorInfo.FileName ?? "Unknown"}";

            if (ErrorInfo.LineNumber.HasValue)
            {
                locationText += $":{ErrorInfo.LineNumber}";

                if (ErrorInfo.ColumnNumber.HasValue)
                {
                    locationText += $":{ErrorInfo.ColumnNumber}";
                }
            }

            var textPos = new Vector2D<float>(contentX, currentY);

            yield return DrawTextCustom(
                _theme.FontName,
                locationText,
                ContentFontSize,
                textPos,
                color: _theme.ScriptErrorLabelColor,
                depth: NextDepth()
            );

            var size = TextMeasurement.MeasureString(_assetManager, locationText, _theme.FontName, ContentFontSize);
            currentY += size.Y + Spacing * 2;
        }

        // Stack trace
        if (!string.IsNullOrEmpty(ErrorInfo.StackTrace))
        {
            var stackTraceLabel = "Stack Trace:";
            var textPos = new Vector2D<float>(contentX, currentY);

            yield return DrawTextCustom(
                _theme.FontName,
                stackTraceLabel,
                ContentFontSize,
                textPos,
                color: _theme.ScriptErrorLabelColor,
                depth: NextDepth()
            );

            var size = TextMeasurement.MeasureString(_assetManager, stackTraceLabel, _theme.FontName, ContentFontSize);
            currentY += size.Y + Spacing;

            // Draw stack trace in a scrollable box
            var maxStackHeight = (int)(dialogBounds.Origin.Y +
                                       dialogBounds.Size.Y -
                                       currentY -
                                       ButtonHeight -
                                       DialogPadding * 2 -
                                       Spacing);
            var stackTraceBounds = new Rectangle<int>(
                (int)contentX,
                (int)currentY,
                maxWidth,
                maxStackHeight
            );

            foreach (var cmd in DrawStackTraceBox(stackTraceBounds))
            {
                yield return cmd;
            }
        }
    }

    /// <summary>
    /// Draws the stack trace box with scrolling support.
    /// </summary>
    private IEnumerable<RenderCommand> DrawStackTraceBox(Rectangle<int> bounds)
    {
        var boundsFloat = new Rectangle<float>(bounds.Origin.X, bounds.Origin.Y, bounds.Size.X, bounds.Size.Y);

        // Draw background
        yield return DrawRectangle(boundsFloat, _theme.ScriptErrorCodeBackgroundColor, depth: NextDepth());

        // Draw border
        foreach (var cmd in DrawBorder(boundsFloat, _theme.ScriptErrorCodeBorderColor, 1f, depth: NextDepth()))
        {
            yield return cmd;
        }

        // Enable scissor test to clip content
        yield return RenderCommandHelpers.CreateScissor(bounds);

        // Draw stack trace lines with scrolling
        var lines = ErrorInfo.StackTrace.Split('\n');
        var textX = bounds.Origin.X + Spacing / 2;
        var textY = bounds.Origin.Y + Spacing / 2 - _stackTraceScrollOffset;

        foreach (var line in lines)
        {
            if (textY + LineHeight > bounds.Origin.Y)
            {
                var textPos = new Vector2D<float>(textX, textY);

                yield return DrawTextCustom(
                    _theme.FontName,
                    line,
                    ContentFontSize,
                    textPos,
                    color: _theme.ScriptErrorCodeTextColor,
                    depth: NextDepth()
                );
            }
            textY += LineHeight;
        }

        // Calculate max scroll
        _stackTraceMaxScroll = Math.Max(0, lines.Length * LineHeight - bounds.Size.Y + Spacing);

        // Disable scissor test
        yield return RenderCommandHelpers.CreateDisableScissor();
    }

    /// <summary>
    /// Draws the close button.
    /// </summary>
    private IEnumerable<RenderCommand> DrawCloseButton()
    {
        var buttonColor = _isCloseButtonHovered ? new Color4b(90, 90, 90) : new Color4b(70, 70, 70);
        var boundsFloat = new Rectangle<float>(
            _closeButtonBounds.Origin.X,
            _closeButtonBounds.Origin.Y,
            _closeButtonBounds.Size.X,
            _closeButtonBounds.Size.Y
        );

        // Draw button background
        yield return DrawRectangle(boundsFloat, buttonColor, depth: NextDepth());

        // Draw button border
        foreach (var cmd in DrawBorder(boundsFloat, new Color4b(100, 100, 100), 1f, depth: NextDepth()))
        {
            yield return cmd;
        }

        // Draw button text
        var text = "Close";
        var textSize = TextMeasurement.MeasureString(_assetManager, text, _theme.FontName, ContentFontSize);
        var textPosition = new Vector2D<float>(
            _closeButtonBounds.Origin.X + (_closeButtonBounds.Size.X - textSize.X) / 2f,
            _closeButtonBounds.Origin.Y + (_closeButtonBounds.Size.Y - textSize.Y) / 2f
        );

        yield return DrawTextCustom(
            _theme.FontName,
            text,
            ContentFontSize,
            textPosition,
            color: Color4b.White,
            depth: NextDepth()
        );
    }

    /// <summary>
    /// Wraps text to fit within a specified width.
    /// </summary>
    private string WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testWidth = TextMeasurement.MeasureStringWidth(_assetManager, testLine, _theme.FontName, ContentFontSize);

            if (testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return string.Join("\n", lines);
    }
}
