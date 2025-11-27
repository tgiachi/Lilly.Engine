using System.ComponentModel;
using Lilly.Engine.GameObjects.Utils;
using Lilly.Engine.Utils;
using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Theme;

/// <summary>
/// Defines a theme for UI components with common colors, fonts, and styling properties.
/// Allows consistent theming across all UI elements in the application.
/// Uses Fody.PropertyChanged for automatic property change notifications.
/// </summary>
public class UITheme : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Event raised when any property of the theme changes.
    /// All subscribed UI components will be notified and can update themselves.
    /// </summary>
    public event EventHandler? ThemeChanged;

    /// <summary>
    /// Gets or sets the default font name for all UI components.
    /// </summary>
    public string FontName { get; set; } = DefaultFonts.DefultFontName;

    /// <summary>
    /// Gets or sets the default font size for all UI components.
    /// </summary>
    public int FontSize { get; set; } = DefaultSizes.DefaultUiFontSize;

    // ========== PRIMARY COLORS ==========

    /// <summary>
    /// Gets or sets the primary text color used across UI components.
    /// </summary>
    public Color4b TextColor { get; set; } = Color4b.Black;

    /// <summary>
    /// Gets or sets the background color for UI components in their normal state.
    /// </summary>
    public Color4b BackgroundColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the background color when a component has focus.
    /// </summary>
    public Color4b BackgroundColorFocused { get; set; } = new(220, 230, 255);

    /// <summary>
    /// Gets or sets the background color for selected/checked state.
    /// </summary>
    public Color4b BackgroundColorSelected { get; set; } = new(100, 150, 255);

    /// <summary>
    /// Gets or sets the border color for UI components.
    /// </summary>
    public Color4b BorderColor { get; set; } = Color4b.DarkGray;

    /// <summary>
    /// Gets or sets the border color when a component has focus.
    /// </summary>
    public Color4b BorderColorFocused { get; set; } = Color4b.Blue;

    /// <summary>
    /// Gets or sets the border color for selected/checked state.
    /// </summary>
    public Color4b BorderColorSelected { get; set; } = new(50, 100, 200);

    // ========== INTERACTIVE COLORS ==========

    /// <summary>
    /// Gets or sets the color for hovered items (ListBox, ComboBox).
    /// </summary>
    public Color4b ItemHoveredColor { get; set; } = new(200, 220, 255);

    /// <summary>
    /// Gets or sets the color for selected items (ListBox, ComboBox).
    /// </summary>
    public Color4b ItemSelectedColor { get; set; } = new(100, 150, 255);

    // ========== COMPONENT-SPECIFIC COLORS ==========

    /// <summary>
    /// Gets or sets the disabled/placeholder text color.
    /// </summary>
    public Color4b PlaceholderTextColor { get; set; } = Color4b.Gray;

    /// <summary>
    /// Gets or sets the progress bar fill color.
    /// </summary>
    public Color4b ProgressBarColor { get; set; } = new(50, 150, 50);

    /// <summary>
    /// Gets or sets the progress bar background color.
    /// </summary>
    public Color4b ProgressBarBackgroundColor { get; set; } = new(200, 200, 200);

    /// <summary>
    /// Gets or sets the scrollbar track color.
    /// </summary>
    public Color4b ScrollBarTrackColor { get; set; } = Color4b.LightGray;

    /// <summary>
    /// Gets or sets the scrollbar thumb color.
    /// </summary>
    public Color4b ScrollBarThumbColor { get; set; } = Color4b.DarkGray;

    /// <summary>
    /// Gets or sets the scrollbar thumb color when hovered.
    /// </summary>
    public Color4b ScrollBarThumbColorHovered { get; set; } = Color4b.Gray;

    /// <summary>
    /// Gets or sets the scrollbar thumb color when dragged.
    /// </summary>
    public Color4b ScrollBarThumbColorDragged { get; set; } = new(100, 100, 100);

    /// <summary>
    /// Gets or sets the dropdown background color (ComboBox).
    /// </summary>
    public Color4b DropdownBackgroundColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the window background color (Window title bar and body).
    /// </summary>
    public Color4b WindowBackgroundColor { get; set; } = new(220, 220, 220);

    // ========== SCRIPT ERROR DIALOG COLORS ==========

    /// <summary>
    /// Gets or sets the script error dialog overlay color (semi-transparent).
    /// </summary>
    public Color4b ScriptErrorOverlayColor { get; set; } = new(0, 0, 0, 180);

    /// <summary>
    /// Gets or sets the script error dialog background color.
    /// </summary>
    public Color4b ScriptErrorDialogBackgroundColor { get; set; } = new(40, 40, 40);

    /// <summary>
    /// Gets or sets the script error dialog title bar color.
    /// </summary>
    public Color4b ScriptErrorTitleBarColor { get; set; } = new(200, 50, 50);

    /// <summary>
    /// Gets or sets the script error dialog title text color.
    /// </summary>
    public Color4b ScriptErrorTitleTextColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the script error dialog label color.
    /// </summary>
    public Color4b ScriptErrorLabelColor { get; set; } = new(200, 200, 200);

    /// <summary>
    /// Gets or sets the script error text color.
    /// </summary>
    public Color4b ScriptErrorTextColor { get; set; } = new(255, 100, 100);

    /// <summary>
    /// Gets or sets the script error stack trace background color.
    /// </summary>
    public Color4b ScriptErrorCodeBackgroundColor { get; set; } = new(30, 30, 30);

    /// <summary>
    /// Gets or sets the script error stack trace border color.
    /// </summary>
    public Color4b ScriptErrorCodeBorderColor { get; set; } = new(60, 60, 60);

    /// <summary>
    /// Gets or sets the script error stack trace text color.
    /// </summary>
    public Color4b ScriptErrorCodeTextColor { get; set; } = new(180, 180, 180);

    // ========== BORDER SETTINGS ==========

    /// <summary>
    /// Gets or sets the border thickness for all UI components.
    /// </summary>
    public int BorderThickness { get; set; } = 2;

    /// <summary>
    /// Gets or sets the border thickness for scrollbars.
    /// </summary>
    public int ScrollBarBorderThickness { get; set; } = 1;

    public UITheme()
    {
        PropertyChanged += (sender, args) =>
                           {
                               ThemeChanged?.Invoke(this, EventArgs.Empty);
                           };
    }

    // ========== PREDEFINED THEMES ==========

    /// <summary>
    /// Gets the default light theme.
    /// </summary>
    public static UITheme Default
        => new()
        {
            FontName = DefaultFonts.DefultFontName,
            FontSize = DefaultSizes.DefaultUiFontSize,
            TextColor = Color4b.Black,
            BackgroundColor = Color4b.White,
            BackgroundColorFocused = new(220, 230, 255),
            BackgroundColorSelected = new(100, 150, 255),
            BorderColor = Color4b.DarkGray,
            BorderColorFocused = Color4b.Blue,
            BorderColorSelected = new(50, 100, 200),
            ItemHoveredColor = new(200, 220, 255),
            ItemSelectedColor = new(100, 150, 255),
            PlaceholderTextColor = Color4b.Gray,
            ProgressBarColor = new(50, 150, 50),
            ProgressBarBackgroundColor = new(200, 200, 200),
            ScrollBarTrackColor = Color4b.LightGray,
            ScrollBarThumbColor = Color4b.DarkGray,
            ScrollBarThumbColorHovered = Color4b.Gray,
            ScrollBarThumbColorDragged = new(100, 100, 100),
            DropdownBackgroundColor = Color4b.White,
            WindowBackgroundColor = new(220, 220, 220),
            ScriptErrorOverlayColor = new(0, 0, 0, 180),
            ScriptErrorDialogBackgroundColor = new(40, 40, 40),
            ScriptErrorTitleBarColor = new(200, 50, 50),
            ScriptErrorTitleTextColor = Color4b.White,
            ScriptErrorLabelColor = new(200, 200, 200),
            ScriptErrorTextColor = new(255, 100, 100),
            ScriptErrorCodeBackgroundColor = new(30, 30, 30),
            ScriptErrorCodeBorderColor = new(60, 60, 60),
            ScriptErrorCodeTextColor = new(180, 180, 180),
            BorderThickness = 2,
            ScrollBarBorderThickness = 1
        };

    /// <summary>
    /// Gets a dark theme with light text on dark backgrounds.
    /// </summary>
    public static UITheme Dark
        => new()
        {
            FontName = DefaultFonts.DefultFontName,
            FontSize = DefaultSizes.DefaultUiFontSize,
            TextColor = Color4b.WhiteSmoke,
            BackgroundColor = new(40, 40, 40),
            BackgroundColorFocused = new(60, 60, 80),
            BackgroundColorSelected = new(100, 100, 150),
            BorderColor = new(80, 80, 80),
            BorderColorFocused = new(100, 150, 255),
            BorderColorSelected = new(150, 200, 255),
            ItemHoveredColor = new(80, 80, 120),
            ItemSelectedColor = new(100, 100, 150),
            PlaceholderTextColor = new(150, 150, 150),
            ProgressBarColor = new(100, 200, 100),
            ProgressBarBackgroundColor = new(100, 100, 100),
            ScrollBarTrackColor = new(60, 60, 60),
            ScrollBarThumbColor = new(120, 120, 120),
            ScrollBarThumbColorHovered = new(150, 150, 150),
            ScrollBarThumbColorDragged = new(180, 180, 180),
            DropdownBackgroundColor = new(50, 50, 50),
            WindowBackgroundColor = new(60, 60, 60),
            ScriptErrorOverlayColor = new(0, 0, 0, 200),
            ScriptErrorDialogBackgroundColor = new(30, 30, 30),
            ScriptErrorTitleBarColor = new(200, 80, 80),
            ScriptErrorTitleTextColor = Color4b.White,
            ScriptErrorLabelColor = new(220, 220, 220),
            ScriptErrorTextColor = new(255, 120, 120),
            ScriptErrorCodeBackgroundColor = new(20, 20, 20),
            ScriptErrorCodeBorderColor = new(50, 50, 50),
            ScriptErrorCodeTextColor = new(200, 200, 200),
            BorderThickness = 2,
            ScrollBarBorderThickness = 1
        };

    /// <summary>
    /// Gets a minimal light theme with subtle colors.
    /// </summary>
    public static UITheme Minimal
        => new()
        {
            FontName = DefaultFonts.DefultFontName,
            FontSize = DefaultSizes.DefaultUiFontSize,
            TextColor = Color4b.Black,
            BackgroundColor = Color4b.White,
            BackgroundColorFocused = new(245, 245, 250),
            BackgroundColorSelected = new(200, 220, 255),
            BorderColor = new(200, 200, 200),
            BorderColorFocused = new(150, 150, 200),
            BorderColorSelected = new(100, 150, 200),
            ItemHoveredColor = new(240, 240, 245),
            ItemSelectedColor = new(200, 220, 255),
            PlaceholderTextColor = new(180, 180, 180),
            ProgressBarColor = new(100, 180, 100),
            ProgressBarBackgroundColor = new(230, 230, 230),
            ScrollBarTrackColor = new(240, 240, 240),
            ScrollBarThumbColor = new(180, 180, 180),
            ScrollBarThumbColorHovered = new(150, 150, 150),
            ScrollBarThumbColorDragged = new(120, 120, 120),
            DropdownBackgroundColor = Color4b.White,
            WindowBackgroundColor = new(240, 240, 240),
            ScriptErrorOverlayColor = new(0, 0, 0, 150),
            ScriptErrorDialogBackgroundColor = new(50, 50, 50),
            ScriptErrorTitleBarColor = new(180, 60, 60),
            ScriptErrorTitleTextColor = Color4b.White,
            ScriptErrorLabelColor = new(190, 190, 190),
            ScriptErrorTextColor = new(240, 100, 100),
            ScriptErrorCodeBackgroundColor = new(40, 40, 40),
            ScriptErrorCodeBorderColor = new(80, 80, 80),
            ScriptErrorCodeTextColor = new(170, 170, 170),
            BorderThickness = 1,
            ScrollBarBorderThickness = 1
        };

    /// <summary>
    /// Gets a vibrant theme with bold colors.
    /// </summary>
    public static UITheme Vibrant
        => new()
        {
            FontName = DefaultFonts.DefultFontName,
            FontSize = DefaultSizes.DefaultUiFontSize,
            TextColor = new(30, 30, 40),
            BackgroundColor = new(255, 255, 250),
            BackgroundColorFocused = new(240, 248, 255),
            BackgroundColorSelected = new(70, 130, 255),
            BorderColor = new(100, 100, 200),
            BorderColorFocused = new(0, 100, 255),
            BorderColorSelected = new(0, 80, 200),
            ItemHoveredColor = new(200, 230, 255),
            ItemSelectedColor = new(70, 130, 255),
            PlaceholderTextColor = new(150, 150, 180),
            ProgressBarColor = new(0, 180, 100),
            ProgressBarBackgroundColor = new(220, 220, 220),
            ScrollBarTrackColor = new(245, 245, 250),
            ScrollBarThumbColor = new(100, 150, 255),
            ScrollBarThumbColorHovered = new(70, 130, 255),
            ScrollBarThumbColorDragged = new(50, 100, 200),
            DropdownBackgroundColor = new(255, 255, 250),
            WindowBackgroundColor = new(230, 240, 255),
            ScriptErrorOverlayColor = new(0, 0, 0, 180),
            ScriptErrorDialogBackgroundColor = new(35, 35, 45),
            ScriptErrorTitleBarColor = new(220, 70, 70),
            ScriptErrorTitleTextColor = Color4b.White,
            ScriptErrorLabelColor = new(210, 210, 220),
            ScriptErrorTextColor = new(255, 120, 100),
            ScriptErrorCodeBackgroundColor = new(25, 25, 35),
            ScriptErrorCodeBorderColor = new(70, 70, 100),
            ScriptErrorCodeTextColor = new(190, 190, 210),
            BorderThickness = 2,
            ScrollBarBorderThickness = 1
        };

    /// <summary>
    /// Creates a copy of this theme.
    /// </summary>
    public UITheme Clone()
        => new()
        {
            FontName = FontName,
            FontSize = FontSize,
            TextColor = TextColor,
            BackgroundColor = BackgroundColor,
            BackgroundColorFocused = BackgroundColorFocused,
            BackgroundColorSelected = BackgroundColorSelected,
            BorderColor = BorderColor,
            BorderColorFocused = BorderColorFocused,
            BorderColorSelected = BorderColorSelected,
            ItemHoveredColor = ItemHoveredColor,
            ItemSelectedColor = ItemSelectedColor,
            PlaceholderTextColor = PlaceholderTextColor,
            ProgressBarColor = ProgressBarColor,
            ProgressBarBackgroundColor = ProgressBarBackgroundColor,
            ScrollBarTrackColor = ScrollBarTrackColor,
            ScrollBarThumbColor = ScrollBarThumbColor,
            ScrollBarThumbColorHovered = ScrollBarThumbColorHovered,
            ScrollBarThumbColorDragged = ScrollBarThumbColorDragged,
            DropdownBackgroundColor = DropdownBackgroundColor,
            WindowBackgroundColor = WindowBackgroundColor,
            ScriptErrorOverlayColor = ScriptErrorOverlayColor,
            ScriptErrorDialogBackgroundColor = ScriptErrorDialogBackgroundColor,
            ScriptErrorTitleBarColor = ScriptErrorTitleBarColor,
            ScriptErrorTitleTextColor = ScriptErrorTitleTextColor,
            ScriptErrorLabelColor = ScriptErrorLabelColor,
            ScriptErrorTextColor = ScriptErrorTextColor,
            ScriptErrorCodeBackgroundColor = ScriptErrorCodeBackgroundColor,
            ScriptErrorCodeBorderColor = ScriptErrorCodeBorderColor,
            ScriptErrorCodeTextColor = ScriptErrorCodeTextColor,
            BorderThickness = BorderThickness,
            ScrollBarBorderThickness = ScrollBarBorderThickness
        };

    /// <summary>
    /// Called by Fody.PropertyChanged when any property changes.
    /// Raises the ThemeChanged event to notify all subscribers.
    /// </summary>
    private void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }
}
