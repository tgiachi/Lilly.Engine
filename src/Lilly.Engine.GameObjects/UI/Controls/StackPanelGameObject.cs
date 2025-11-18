using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Features;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Silk.NET.Maths;
using TrippyGL;

namespace Lilly.Engine.GameObjects.UI.Controls;

/// <summary>
/// Arranges child elements into a single line that can be oriented horizontally or vertically.
/// Similar to WPF/XAML StackPanel.
/// </summary>
public class StackPanelGameObject : BaseGameObject2D
{
    private Orientation _orientation = Orientation.Vertical;
    private int _spacing = 5;
    private int _padding;
    private bool _autoSize = true;
    private int _width = 200;
    private int _height = 200;

    /// <summary>
    /// Gets or sets the orientation of the stack panel (Vertical or Horizontal).
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the spacing between child elements in pixels.
    /// </summary>
    public int Spacing
    {
        get => _spacing;
        set
        {
            if (_spacing != value)
            {
                _spacing = Math.Max(0, value);
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the padding around the content in pixels.
    /// </summary>
    public int Padding
    {
        get => _padding;
        set
        {
            if (_padding != value)
            {
                _padding = Math.Max(0, value);
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the panel automatically resizes to fit its children.
    /// </summary>
    public bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (_autoSize != value)
            {
                _autoSize = value;
                InvalidateLayout();
            }
        }
    }

    /// <summary>
    /// Gets or sets the width of the panel (only used when AutoSize is false).
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = Math.Max(0, value);

                if (!_autoSize)
                {
                    InvalidateLayout();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the height of the panel (only used when AutoSize is false).
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = Math.Max(0, value);

                if (!_autoSize)
                {
                    InvalidateLayout();
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the background color of the panel (null for transparent).
    /// </summary>
    public Color4b? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the border color of the panel (null for no border).
    /// </summary>
    public Color4b? BorderColor { get; set; }

    /// <summary>
    /// Gets or sets the border thickness in pixels.
    /// </summary>
    public int BorderThickness { get; set; }

    /// <summary>
    /// Gets or sets the UI theme used for default styling.
    /// </summary>
    public UITheme? Theme { get; set; }

    /// <summary>
    /// Adds a child game object to the stack panel.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(IGameObject2D child)
    {
        ArgumentNullException.ThrowIfNull(child);

        InvalidateLayout();
    }

    /// <summary>
    /// Inserts a child game object at the specified index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="child">The child to insert.</param>
    public void InsertChild(int index, IGameObject2D child)
    {
        AddChild(child);
        InvalidateLayout();
    }

    /// <summary>
    /// Removes a child game object from the stack panel.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    /// <returns>True if the child was removed, false if not found.</returns>
    public bool RemoveChild(IGameObject2D child)
    {
        var removed = Children.Remove(child);

        if (removed)
        {
            InvalidateLayout();
        }

        return removed;
    }

    /// <summary>
    /// Removes the child at the specified index.
    /// </summary>
    /// <param name="index">The index of the child to remove.</param>
    public void RemoveChildAt(int index)
    {
        InvalidateLayout();
    }

    /// <summary>
    /// Removes all children from the stack panel.
    /// </summary>
    public void ClearChildren()
    {
        Children.Clear();
        InvalidateLayout();
    }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        // Draw background if specified
        if (BackgroundColor.HasValue)
        {
            var panelSize = CalculatePanelSize();

            yield return DrawRectangle(
                new(Transform.Position, new(panelSize.X, panelSize.Y)),
                BackgroundColor.Value,
                NextDepth()
            );
        }

        // Draw border if specified
        if (BorderColor.HasValue && BorderThickness > 0)
        {
            var panelSize = CalculatePanelSize();

            foreach (var cmd in DrawHollowRectangle(
                         Transform.Position,
                         new(panelSize.X, panelSize.Y),
                         BorderColor.Value,
                         BorderThickness,
                         NextDepth()
                     ))
            {
                yield return cmd;
            }
        }

        // Children draw themselves with their updated positions
        // Note: Children are rendered by the parent scene/layer system
    }

    /// <summary>
    /// Recalculates and updates the layout of all children.
    /// </summary>
    private void InvalidateLayout()
    {
        var currentPos = _orientation == Orientation.Vertical
                             ? new Vector2D<float>(Transform.Position.X + _padding, Transform.Position.Y + _padding)
                             : new Vector2D<float>(Transform.Position.X + _padding, Transform.Position.Y + _padding);

        foreach (IGameObject2D child in Children)
        {
            // Set child position
            child.Transform.Position = currentPos;

            // Calculate next position based on orientation
            if (_orientation == Orientation.Vertical)
            {
                currentPos = new Vector2D<float>(
                    currentPos.X,
                    currentPos.Y + child.Transform.Size.Y + _spacing
                );
            }
            else // Horizontal
            {
                currentPos = new Vector2D<float>(
                    currentPos.X + child.Transform.Size.X + _spacing,
                    currentPos.Y
                );
            }
        }

        // Update panel size if AutoSize is enabled
        if (_autoSize)
        {
            var size = CalculatePanelSize();
            Transform.Size = new(size.X, size.Y);
        }
        else
        {
            Transform.Size = new(_width, _height);
        }
    }

    /// <summary>
    /// Calculates the total size needed for the panel based on its children.
    /// </summary>
    private Vector2D<int> CalculatePanelSize()
    {
        if (Children.Count == 0)
        {
            return new Vector2D<int>(_padding * 2, _padding * 2);
        }

        int width = 0;
        int height = 0;

        if (_orientation == Orientation.Vertical)
        {
            // For vertical stack: width = max child width, height = sum of heights + spacing
            foreach (IGameObject2D child in Children)
            {
                width = Math.Max(width, (int)child.Transform.Size.X);
                height += (int)child.Transform.Size.Y;
            }

            // Add spacing between items (n-1 spacings for n items)
            height += (Children.Count - 1) * _spacing;
        }
        else // Horizontal
        {
            // For horizontal stack: width = sum of widths + spacing, height = max child height
            foreach (IGameObject2D child in Children)
            {
                width += (int)child.Transform.Size.X;
                height = Math.Max(height, (int)child.Transform.Size.Y);
            }

            // Add spacing between items (n-1 spacings for n items)
            width += (Children.Count - 1) * _spacing;
        }

        // Add padding
        width += _padding * 2;
        height += _padding * 2;

        return new Vector2D<int>(width, height);
    }
}

/// <summary>
/// Specifies the orientation of a StackPanel.
/// </summary>
public enum Orientation
{
    /// <summary>
    /// Arranges children vertically (top to bottom).
    /// </summary>
    Vertical,

    /// <summary>
    /// Arranges children horizontally (left to right).
    /// </summary>
    Horizontal
}
