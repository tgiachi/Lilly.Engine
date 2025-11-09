using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Primitives;

namespace Lilly.Engine.Rendering.Core.Base.GameObjects;

/// <summary>
/// Base class for 3D game objects.
/// </summary>
public abstract class BaseGameObject3D : IGameObject3D
{
    /// <summary>
    /// Gets or sets the parent game object.
    /// </summary>
    public IGameObject? Parent { get; set; }

    /// <summary>
    /// Gets the collection of child game objects.
    /// </summary>
    public GameObjectCollection<IGameObject> Children { get; } = new();

    /// <summary>
    /// Gets or sets the unique identifier for the game object.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the game object.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rendering order of the game object.
    /// </summary>
    public ushort Order { get; set; }

    /// <summary>
    /// Gets the 3D transform for position, rotation, and scale.
    /// </summary>
    public Transform3D Transform { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the game object is visible and should be rendered.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the layer of the game object.
    /// </summary>
    public int Layer { get; set; }

    /// <summary>
    /// Renders the game object and its children.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <param name="renderCommands">The list of render commands to populate.</param>
    public void Render(GameTime gameTime, ref List<RenderCommand> renderCommands)
    {
        if (!IsVisible)
        {
            return;
        }

        Draw(gameTime, ref renderCommands);

        foreach (var child in Children)
        {
            child.Render(gameTime, ref renderCommands);
        }
    }

    /// <summary>
    /// Draws the game object by adding render commands to the list.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <param name="renderCommands">The list of render commands to populate.</param>
    public abstract void Draw(GameTime gameTime, ref List<RenderCommand> renderCommands);
}
