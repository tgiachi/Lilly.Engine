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
/// A focusable UI combo box component that displays a dropdown list of selectable items.
/// Allows users to select from a predefined list of options.
/// </summary>
public class ComboBoxGameObject : BaseGameObject2D, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;
    private bool _hasFocus;
    private bool _isMouseInBounds;
    private int _width = 200;
    private int _height = 30;
    private Vector2 _lastMousePos = Vector2.Zero;

    private const int BorderThickness = 2;
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
    public bool IsOpen { get; set; }

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
    public ComboBoxGameObject(IInputManagerService inputManager, IAssetManager assetManager, UITheme theme)
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
        _lastMousePos = mousePos;
        var wasInBounds = _isMouseInBounds;
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

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        var bounds = Bounds;
        var bgColor = HasFocus ? Theme.BackgroundColorFocused : Theme.BackgroundColor;
        var brColor = HasFocus ? Theme.BorderColorFocused : Theme.BorderColor;

        // Draw closed combo box background
        yield return DrawRectangle(
            new(Transform.Position, new(_width, _height)),
            bgColor,
            NextDepth()
        );

        // Draw border
        foreach (var cmd in DrawHollowRectangle(
                     Transform.Position,
                     new(_width, _height),
                     brColor,
                     Theme.BorderThickness,
                     NextDepth()
                 ))
        {
            yield return cmd;
        }

        // Draw selected item text
        if (_selectedIndex >= 0)
        {
            var textPos = new Vector2D<float>(
                Transform.Position.X + Padding,
                Transform.Position.Y + Padding
            );

            yield return DrawTextCustom(
                Theme.FontName,
                Items[_selectedIndex],
                Theme.FontSize,
                textPos,
                color: Theme.TextColor,
                depth: NextDepth()
            );
        }

        // Draw dropdown indicator arrow
        foreach (var cmd in DrawDropdownArrow())
        {
            yield return cmd;
        }

        // Draw dropdown list if open
        if (IsOpen)
        {
            foreach (var cmd in DrawDropdown(brColor))
            {
                yield return cmd;
            }
        }
    }

    private IEnumerable<RenderCommand> DrawDropdown(Color4b borderColor)
    {
        if (Items.Count == 0)
        {
            yield break;
        }

        var dropdownBounds = GetDropdownBounds();

        // Draw dropdown background
        yield return DrawRectangle(
            new(
                new(dropdownBounds.Origin.X, dropdownBounds.Origin.Y),
                new(dropdownBounds.Size.X, dropdownBounds.Size.Y)
            ),
            Theme.BackgroundColor,
            NextDepth()
        );

        // Draw border
        foreach (var cmd in DrawHollowRectangle(
                     new(dropdownBounds.Origin.X, dropdownBounds.Origin.Y),
                     new(dropdownBounds.Size.X, dropdownBounds.Size.Y),
                     borderColor,
                     Theme.BorderThickness,
                     NextDepth()
                 ))
        {
            yield return cmd;
        }

        // Draw items
        for (var i = 0; i < Items.Count && i * ItemHeight < dropdownBounds.Size.Y; i++)
        {
            var itemBounds = new Rectangle<float>(
                new(dropdownBounds.Origin.X, dropdownBounds.Origin.Y + i * ItemHeight),
                new(dropdownBounds.Size.X, ItemHeight)
            );

            // Draw item background
            var itemBgColor = i == _selectedIndex ? Theme.ItemSelectedColor :
                              i == _hoveredIndex ? Theme.ItemHoveredColor :
                              Theme.BackgroundColor;

            yield return DrawRectangle(itemBounds, itemBgColor, NextDepth());

            // Draw item text
            var textPos = new Vector2D<float>(
                dropdownBounds.Origin.X + Padding,
                dropdownBounds.Origin.Y + i * ItemHeight + Padding
            );

            yield return DrawTextCustom(
                Theme.FontName,
                Items[i],
                Theme.FontSize,
                textPos,
                color: Theme.TextColor,
                depth: NextDepth()
            );
        }
    }

    private IEnumerable<RenderCommand> DrawDropdownArrow()
    {
        var bounds = Bounds;
        var arrowSize = 8;
        var arrowX = bounds.Origin.X + bounds.Size.X - Padding - arrowSize;
        var arrowY = bounds.Origin.Y + (bounds.Size.Y - arrowSize) / 2;

        if (IsOpen)
        {
            // Up arrow: ▲
            yield return DrawRectangle(
                new(new(arrowX, arrowY), new(arrowSize, 1)),
                Theme.TextColor,
                NextDepth()
            );
            yield return DrawRectangle(
                new(new(arrowX + arrowSize / 2f, arrowY), new(1, arrowSize)),
                Theme.TextColor,
                NextDepth()
            );
        }
        else
        {
            // Down arrow: ▼
            yield return DrawRectangle(
                new(new(arrowX, arrowY), new(arrowSize, 1)),
                Theme.TextColor,
                NextDepth()
            );
            yield return DrawRectangle(
                new(new(arrowX + arrowSize / 2f, arrowY), new(1, arrowSize)),
                Theme.TextColor,
                NextDepth()
            );
        }
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
        Transform.Size = new(_width, _height);
    }
}
