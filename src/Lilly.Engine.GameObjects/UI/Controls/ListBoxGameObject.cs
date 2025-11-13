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
/// A focusable UI list box component that displays a scrollable list of selectable items.
/// Supports both single and multiple selection modes.
/// </summary>
public class ListBoxGameObject : BaseGameObject2D, IInputReceiver
{
    private readonly IInputManagerService _inputManager;
    private readonly IAssetManager _assetManager;

    private readonly HashSet<int> _selectedIndices = new();
    private int _hoveredIndex = -1;
    private int _scrollOffset; // For scrolling when items exceed visible area
    private bool _hasFocus;
    private bool _isMouseInBounds;
    private int _width = 200;
    private int _height = 200;

    private const int BorderThickness = 2;
    private const int ItemHeight = 30;
    private const int Padding = 4;

    /// <summary>
    /// Gets the list of items in the list box.
    /// </summary>
    public List<string> Items { get; } = new();

    /// <summary>
    /// Gets or sets whether multiple items can be selected.
    /// </summary>
    public bool MultiSelect { get; set; }

    /// <summary>
    /// Gets the currently selected item index (first selected in multi-select mode).
    /// Returns -1 if nothing is selected.
    /// </summary>
    public int SelectedIndex => _selectedIndices.Count > 0 ? _selectedIndices.Min() : -1;

    /// <summary>
    /// Gets the currently selected item text (first selected in multi-select mode).
    /// Returns empty string if nothing is selected.
    /// </summary>
    public string SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : string.Empty;

    /// <summary>
    /// Gets the collection of selected item indices.
    /// </summary>
    public IReadOnlyCollection<int> SelectedIndices => _selectedIndices;

    /// <summary>
    /// Gets or sets the UI theme used for styling.
    /// </summary>
    public UITheme Theme { get; }

    /// <summary>
    /// Gets or sets the color of the text.
    /// </summary>
    public Color4b TextColor { get; set; } = Color4b.Black;

    /// <summary>
    /// Gets or sets the background color of the list box.
    /// </summary>
    public Color4b BackgroundColor { get; set; } = Color4b.White;

    /// <summary>
    /// Gets or sets the background color when focused.
    /// </summary>
    public Color4b BackgroundColorFocused { get; set; } = new(220, 230, 255);

    /// <summary>
    /// Gets or sets the background color of hovered items.
    /// </summary>
    public Color4b ItemHoveredColor { get; set; } = new(200, 220, 255);

    /// <summary>
    /// Gets or sets the background color of selected items.
    /// </summary>
    public Color4b ItemSelectedColor { get; set; } = new(100, 150, 255);

    /// <summary>
    /// Gets or sets the border color.
    /// </summary>
    public Color4b BorderColor { get; set; } = Color4b.DarkGray;

    /// <summary>
    /// Gets or sets the border color when focused.
    /// </summary>
    public Color4b BorderColorFocused { get; set; } = Color4b.Blue;

    /// <summary>
    /// Gets or sets the list box width.
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
    /// Gets or sets the list box height.
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
    /// Event raised when the selected item changes.
    /// </summary>
    public event EventHandler? OnSelectionChanged;

    /// <summary>
    /// Initializes a new instance of the ListBoxGameObject class.
    /// </summary>
    /// <param name="inputManager">The input manager service.</param>
    /// <param name="assetManager">The asset manager service for loading fonts and textures.</param>
    /// <param name="theme">The theme to apply.</param>
    public ListBoxGameObject(IInputManagerService inputManager, IAssetManager assetManager, UITheme theme)
    {
        _inputManager = inputManager ?? throw new ArgumentNullException(nameof(inputManager));
        _assetManager = assetManager ?? throw new ArgumentNullException(nameof(assetManager));
        Theme = theme ?? throw new ArgumentNullException(nameof(theme));

        UpdateTransformSize();
    }

    /// <summary>
    /// Adds an item to the list box.
    /// </summary>
    /// <param name="item">The item text to add.</param>
    public void AddItem(string item)
    {
        Items.Add(item ?? string.Empty);
    }

    /// <summary>
    /// Clears all items from the list box.
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
        _selectedIndices.Clear();
        _scrollOffset = 0;
    }

    /// <summary>
    /// Clears all selections.
    /// </summary>
    public void ClearSelection()
    {
        if (_selectedIndices.Count > 0)
        {
            _selectedIndices.Clear();
            OnSelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Deselects an item by index.
    /// </summary>
    /// <param name="index">The index to deselect.</param>
    public void DeselectItem(int index)
    {
        if (_selectedIndices.Remove(index))
        {
            OnSelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Removes an item from the list box by index.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    public bool RemoveItem(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            return false;
        }

        Items.RemoveAt(index);
        _selectedIndices.Remove(index);

        // Adjust selected indices for items after the removed one
        var toRemove = _selectedIndices.Where(i => i > index).ToList();

        foreach (var i in toRemove)
        {
            _selectedIndices.Remove(i);
            _selectedIndices.Add(i - 1);
        }

        return true;
    }

    /// <summary>
    /// Selects an item by index.
    /// </summary>
    /// <param name="index">The index to select.</param>
    /// <param name="addToSelection">If true and MultiSelect is enabled, adds to selection. Otherwise replaces.</param>
    public void SelectItem(int index, bool addToSelection = false)
    {
        if (index < 0 || index >= Items.Count)
        {
            return;
        }

        if (!MultiSelect || !addToSelection)
        {
            _selectedIndices.Clear();
        }

        _selectedIndices.Add(index);
        OnSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsFocusable => true;

    public bool HasFocus
    {
        get => _hasFocus;
        set => _hasFocus = value;
    }

    public Rectangle<int> Bounds
        => new(
            new((int)Transform.Position.X, (int)Transform.Position.Y),
            new(_width, _height)
        );

    public void HandleKeyboard(KeyboardState keyboardState, KeyboardState previousKeyboardState, GameTime gameTime)
    {
        if (!HasFocus || Items.Count == 0)
        {
            return;
        }

        var visibleCount = GetVisibleItemCount();
        var currentSelected = SelectedIndex;

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Down))
        {
            var nextIndex = currentSelected >= 0 ? currentSelected + 1 : 0;

            if (nextIndex < Items.Count)
            {
                SelectItem(nextIndex);
                EnsureItemVisible(nextIndex);
            }
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Up))
        {
            var prevIndex = currentSelected > 0 ? currentSelected - 1 : 0;
            SelectItem(prevIndex);
            EnsureItemVisible(prevIndex);
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.Home))
        {
            SelectItem(0);
            EnsureItemVisible(0);
        }

        if (IsKeyJustPressed(keyboardState, previousKeyboardState, Key.End))
        {
            SelectItem(Items.Count - 1);
            EnsureItemVisible(Items.Count - 1);
        }
    }

    public void HandleMouse(MouseState mouseState, GameTime gameTime)
    {
        var mousePos = new Vector2(mouseState.Position.X, mouseState.Position.Y);
        var wasInBounds = _isMouseInBounds;
        _isMouseInBounds = IsMouseInBounds(mousePos);

        var bounds = Bounds;
        var contentArea = new Rectangle<int>(
            new(bounds.Origin.X + BorderThickness, bounds.Origin.Y + BorderThickness),
            new(bounds.Size.X - BorderThickness * 2, bounds.Size.Y - BorderThickness * 2)
        );

        if (_inputManager.IsMouseButtonPressed(MouseButton.Left) && RectContains(contentArea, mousePos))
        {
            if (_hoveredIndex >= 0 && _hoveredIndex < Items.Count)
            {
                // Multi-select is handled via keyboard (Ctrl+click would require keyboard context)
                // For now, single select on click, multi-select via arrow keys + Shift
                SelectItem(_hoveredIndex, false);
            }
        }

        UpdateHoverState(mousePos);
    }

    public void HandleMouseWheel(MouseState mouseState, MouseState previousMouseState, GameTime gameTime)
    {
        // Not implemented for list box
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

        var bounds = Bounds;
        var bgColor = HasFocus ? BackgroundColorFocused : BackgroundColor;
        var brColor = HasFocus ? BorderColorFocused : BorderColor;

        // Draw background
        yield return DrawRectangle(
            new Rectangle<float>(Transform.Position, new Vector2D<float>(_width, _height)),
            bgColor,
            depth: NextDepth()
        );

        // Draw border
        foreach (var cmd in DrawHollowRectangle(
            Transform.Position,
            new Vector2D<float>(_width, _height),
            brColor,
            BorderThickness,
            depth: NextDepth()))
        {
            yield return cmd;
        }

        // Draw items
        var visibleCount = GetVisibleItemCount();

        for (var i = 0; i < visibleCount && _scrollOffset + i < Items.Count; i++)
        {
            var itemIndex = _scrollOffset + i;
            var itemBounds = new Rectangle<float>(
                new Vector2D<float>(
                    bounds.Origin.X + BorderThickness,
                    bounds.Origin.Y + BorderThickness + i * ItemHeight
                ),
                new Vector2D<float>(bounds.Size.X - BorderThickness * 2, ItemHeight)
            );

            // Draw item background
            var itemBgColor = _selectedIndices.Contains(itemIndex) ? ItemSelectedColor :
                              itemIndex == _hoveredIndex ? ItemHoveredColor :
                              BackgroundColor;

            yield return DrawRectangle(itemBounds, itemBgColor, depth: NextDepth());

            // Draw item text
            var textPos = new Vector2D<float>(
                itemBounds.Origin.X + Padding,
                itemBounds.Origin.Y + Padding
            );

            yield return DrawTextCustom(
                Theme.FontName,
                Items[itemIndex],
                Theme.FontSize,
                textPos,
                color: TextColor,
                depth: NextDepth()
            );
        }
    }

    /// <summary>
    /// Ensures that an item at the given index is visible (scrolls if necessary).
    /// </summary>
    private void EnsureItemVisible(int index)
    {
        var visibleCount = GetVisibleItemCount();

        if (index < _scrollOffset)
        {
            _scrollOffset = index;
        }
        else if (index >= _scrollOffset + visibleCount)
        {
            _scrollOffset = index - visibleCount + 1;
        }
    }

    /// <summary>
    /// Gets the number of visible items that fit in the list box.
    /// </summary>
    private int GetVisibleItemCount()
    {
        var contentHeight = _height - BorderThickness * 2;
        return Math.Max(1, contentHeight / ItemHeight);
    }

    private void UpdateHoverState(Vector2 mousePos)
    {
        var bounds = Bounds;
        _hoveredIndex = -1;

        var contentArea = new Rectangle<int>(
            new(bounds.Origin.X + BorderThickness, bounds.Origin.Y + BorderThickness),
            new(bounds.Size.X - BorderThickness * 2, bounds.Size.Y - BorderThickness * 2)
        );

        if (contentArea.Contains(new Vector2D<int>((int)mousePos.X, (int)mousePos.Y)))
        {
            var relativeY = (int)mousePos.Y - contentArea.Origin.Y;
            var itemIndex = _scrollOffset + relativeY / ItemHeight;

            if (itemIndex >= 0 && itemIndex < Items.Count && relativeY >= 0 && relativeY < contentArea.Size.Y)
            {
                _hoveredIndex = itemIndex;
            }
        }
    }

    private void UpdateTransformSize()
    {
        Transform.Size = new Vector2D<float>(_width, _height);
    }

    private static bool IsKeyJustPressed(KeyboardState current, KeyboardState previous, Key key)
    {
        return current.IsKeyPressed(key) && !previous.IsKeyPressed(key);
    }

    private static bool RectContains(Rectangle<int> rect, Vector2 point)
    {
        return rect.Contains(new Vector2D<int>((int)point.X, (int)point.Y));
    }
}
