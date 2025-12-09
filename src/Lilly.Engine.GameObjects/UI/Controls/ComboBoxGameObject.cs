using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Interfaces.Input;
using Lilly.Rendering.Core.Interfaces.Services;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Maths;
using TrippyGL;
using MouseButton = Lilly.Rendering.Core.Types.MouseButton;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// A focusable UI combo box component that displays a dropdown list of selectable items.
/// Allows users to select from a predefined list of options.
/// </summary>
public class ComboBoxGameObject : Base2dGameObject, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;
    private bool _hasFocus;
    private bool _isMouseInBounds;
    private int _width = 200;
    private int _height = 30;
    private bool _isOpen;

    private const int ItemHeight = 30;
    private const int Padding = 4;
    private const int MaxDropdownHeight = 150;
    private const int DefaultScreenHeight = 768;

    /// <summary>
    /// Gets the list of items in the combo box.
    /// </summary>
    public List<string> Items { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected item index.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value != _selectedIndex)
            {
                _selectedIndex = value >= 0 && value < Items.Count ? value : -1;
                OnSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// Gets the currently selected item text, or empty string if nothing selected.
    /// </summary>
    public string SelectedItem => _selectedIndex >= 0 && _selectedIndex < Items.Count ? Items[_selectedIndex] : string.Empty;

    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                UpdateTransformSize();
            }
        }
    }

    /// <summary>
    /// Gets or sets the combo box width.
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
    /// Gets or sets the combo box height.
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
    /// Event raised when the selected item changes.
    /// </summary>
    public event EventHandler? OnSelectionChanged;

    /// <summary>
    /// Initializes a new instance of the ComboBoxGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    /// <param name="assetManager">The asset manager service for loading fonts and textures.</param>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="gameObjectManager">The render pipeline/game object manager.</param>
    public ComboBoxGameObject(
        IInputManagerService inputManager,
        IAssetManager assetManager,
        UITheme theme,
        IRenderPipeline gameObjectManager
    ) : base("ComboBox", gameObjectManager, 100)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));

        UpdateTransformSize();
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

                if (!_hasFocus)
                {
                    IsOpen = false;
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
    /// Adds an item to the combo box.
    /// </summary>
    /// <param name="item">The item text to add.</param>
    public void AddItem(string item)
    {
        Items.Add(item ?? string.Empty);
    }

    /// <summary>
    /// Clears all items from the combo box.
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
        _selectedIndex = -1;
        IsOpen = false;
    }

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus)
        {
            return;
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Down))
        {
            if (!IsOpen)
            {
                IsOpen = true;
            }
            else if (_selectedIndex < Items.Count - 1)
            {
                SelectedIndex = _selectedIndex + 1;
            }
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Up))
        {
            if (IsOpen && _selectedIndex > 0)
            {
                SelectedIndex = _selectedIndex - 1;
            }
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Enter))
        {
            IsOpen = !IsOpen;
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Escape))
        {
            IsOpen = false;
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        _isMouseInBounds = IsMouseInBounds(mousePos);

        // Check if clicking on closed combo box
        if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && _isMouseInBounds && !IsOpen)
        {
            IsOpen = true;
        }

        // Check if clicking on dropdown item
        else if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && IsOpen)
        {
            var dropdownBounds = GetDropdownBounds();

            if (RectContains(dropdownBounds, mousePos))
            {
                var itemIndex = (int)((mousePos.Y - dropdownBounds.Origin.Y) / ItemHeight);

                if (itemIndex >= 0 && itemIndex < Items.Count)
                {
                    SelectedIndex = itemIndex;
                    IsOpen = false;
                }
            }
            else if (!_isMouseInBounds)
            {
                IsOpen = false;
            }
        }

        UpdateHoverState(mousePos);
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented for combo box
    }

    public bool IsMouseInBounds(Vector2 mousePosition)
    {
        var bounds = Bounds;

        return bounds.Contains(new Vector2D<int>((int)mousePosition.X, (int)mousePosition.Y));
    }

    /// <summary>
    /// Removes an item from the combo box by index.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    public bool RemoveItem(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return false;
        }

        Items.RemoveAt(index);

        // Adjust selected index if needed
        if (_selectedIndex >= Items.Count)
        {
            _selectedIndex = Items.Count > 0 ? Items.Count - 1 : -1;
        }

        return true;
    }

    protected override void OnDraw(GameTime gameTime)
    {
        if (!IsActive || SpriteBatcher == null)
        {
            return;
        }

        var bgColor = HasFocus ? Theme.BackgroundColorFocused : Theme.BackgroundColor;
        var brColor = HasFocus ? Theme.BorderColorFocused : Theme.BorderColor;

        // Draw closed combo box background
        SpriteBatcher.DrawRectangle(
            Transform.Position,
            new(_width, _height),
            bgColor
        );

        // Draw border
        SpriteBatcher.DrawHollowRectangle(
            Transform.Position,
            new(_width, _height),
            brColor,
            Theme.BorderThickness
        );

        // Draw selected item text
        if (_selectedIndex >= 0)
        {
            var textPos = new Vector2(
                Transform.Position.X + Padding,
                Transform.Position.Y + (_height - Theme.FontSize) / 2f
            );

            var textAvailableWidth = _width - Padding * 3; // Leave room for arrow
            var text = TrimTextToFit(Items[_selectedIndex], textAvailableWidth);

            SpriteBatcher.DrawText(
                Theme.FontName,
                Theme.FontSize,
                text,
                textPos,
                Theme.TextColor
            );
        }

        // Draw dropdown indicator arrow
        DrawDropdownArrow();

        // Draw dropdown list if open
        if (IsOpen)
        {
            DrawDropdown(brColor);
        }
    }

    private void DrawDropdown(Color4b borderColor)
    {
        if (Items.Count == 0 || SpriteBatcher == null)
        {
            return;
        }

        var dropdownBounds = GetDropdownBounds();
        var dropdownPos = new Vector2(dropdownBounds.Origin.X, dropdownBounds.Origin.Y);
        var dropdownSize = new Vector2(dropdownBounds.Size.X, dropdownBounds.Size.Y);

        // Draw dropdown background
        SpriteBatcher.DrawRectangle(
            dropdownPos,
            dropdownSize,
            Theme.DropdownBackgroundColor
        );

        // Draw border
        SpriteBatcher.DrawHollowRectangle(
            dropdownPos,
            dropdownSize,
            borderColor,
            Theme.BorderThickness
        );

        // Draw items
        var visibleItems = Math.Min(Items.Count, dropdownBounds.Size.Y / ItemHeight);

        for (var i = 0; i < visibleItems; i++)
        {
            var itemPos = new Vector2(dropdownBounds.Origin.X, dropdownBounds.Origin.Y + i * ItemHeight);
            var itemSize = new Vector2(dropdownBounds.Size.X, ItemHeight);

            var itemBgColor = i == _selectedIndex ? Theme.ItemSelectedColor :
                              i == _hoveredIndex ? Theme.ItemHoveredColor :
                              Theme.BackgroundColor;

            SpriteBatcher.DrawRectangle(
                itemPos,
                itemSize,
                itemBgColor
            );

            var textPos = new Vector2(
                dropdownBounds.Origin.X + Padding,
                dropdownBounds.Origin.Y + i * ItemHeight + Padding
            );

            var text = TrimTextToFit(Items[i], itemSize.X - Padding * 2);

            SpriteBatcher.DrawText(
                Theme.FontName,
                Theme.FontSize,
                text,
                textPos,
                Theme.TextColor
            );
        }
    }

    private void DrawDropdownArrow()
    {
        if (SpriteBatcher == null)
        {
            return;
        }

        var arrowSize = 8;
        var arrowX = Transform.Position.X + _width - Padding - arrowSize;
        var arrowY = Transform.Position.Y + (_height - arrowSize) / 2f;

        var arrowPos = new Vector2(arrowX, arrowY);
        var arrowColor = Theme.TextColor;

        // Simple arrow representation using two rectangles (avoids triangle support)
        SpriteBatcher.DrawRectangle(
            arrowPos,
            new(arrowSize, 1),
            arrowColor
        );

        SpriteBatcher.DrawRectangle(
            new(arrowPos.X + arrowSize / 2f, arrowPos.Y),
            new(1, arrowSize),
            arrowColor
        );
    }

    private Rectangle<int> GetDropdownBounds()
    {
        var bounds = Bounds;
        var dropdownHeight = Math.Min(Items.Count * ItemHeight, MaxDropdownHeight);

        // Check if dropdown fits below
        var dropdownY = bounds.Origin.Y + bounds.Size.Y;

        if (dropdownY + dropdownHeight > DefaultScreenHeight)
        {
            // Position above the combo box
            dropdownY = bounds.Origin.Y - dropdownHeight;
        }

        return new(
            new(bounds.Origin.X, dropdownY),
            new(bounds.Size.X, dropdownHeight)
        );
    }

    private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Key key)
        => current.IsKeyPressed(key) && !previous.IsKeyPressed(key);

    private static bool RectContains(Rectangle<int> rect, Vector2 point)
        => rect.Contains(new Vector2D<int>((int)point.X, (int)point.Y));

    private string TrimTextToFit(string text, float maxWidth)
    {
        const string ellipsis = "...";
        var ellipsisWidth = TextMeasurement.MeasureStringWidth(_assetManager, ellipsis, Theme.FontName, Theme.FontSize);

        if (maxWidth <= ellipsisWidth)
        {
            return string.Empty;
        }

        var current = text;

        while (TextMeasurement.MeasureStringWidth(_assetManager, current, Theme.FontName, Theme.FontSize) >
               maxWidth - ellipsisWidth &&
               current.Length > 0)
        {
            current = current[..^1];
        }

        if (current.Length == text.Length)
        {
            return current;
        }

        return current + ellipsis;
    }

    private void UpdateHoverState(Vector2 mousePos)
    {
        if (IsOpen && Items.Count > 0)
        {
            var dropdownBounds = GetDropdownBounds();

            for (var i = 0; i < Items.Count; i++)
            {
                var itemY = dropdownBounds.Origin.Y + i * ItemHeight;
                var itemRect = new Rectangle<int>(
                    new(dropdownBounds.Origin.X, itemY),
                    new(dropdownBounds.Size.X, ItemHeight)
                );

                if (itemRect.Contains(new Vector2D<int>((int)mousePos.X, (int)mousePos.Y)))
                {
                    _hoveredIndex = i;

                    return;
                }
            }

            _hoveredIndex = -1;
        }
    }

    private void UpdateTransformSize()
    {
        if (_isOpen && Items.Count > 0)
        {
            var dropdownBounds = GetDropdownBounds();
            var totalHeight = _height + dropdownBounds.Size.Y;
            Transform.Size = new(_width, totalHeight);
        }
        else
        {
            Transform.Size = new(_width, _height);
        }
    }
}
