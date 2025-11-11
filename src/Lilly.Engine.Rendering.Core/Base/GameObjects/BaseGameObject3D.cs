using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
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
    ///  Draws the game object using the provided camera and game time.
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="gameTime"></param>
    public virtual void Draw(ICamera3D camera, GameTime gameTime)
    {

    }

    /// <summary>
    /// Draws the game object by returning render commands.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <returns>An enumerable collection of render commands for this object.</returns>
    protected abstract IEnumerable<RenderCommand> Draw(GameTime gameTime);

    /// <summary>
    /// Renders the game object and its children by collecting all render commands.
    /// </summary>
    /// <param name="gameTime">The game timing information.</param>
    /// <returns>An enumerable collection of render commands from this object and all its children.</returns>
    public IEnumerable<RenderCommand> Render(GameTime gameTime)
    {
        if (!IsVisible)
        {
            yield break;
        }

        // Yield commands from this object
        foreach (var command in Draw(gameTime))
        {
            yield return command;
        }

        // Yield commands from all children
        foreach (var child in Children)
        {
            foreach (var command in child.Render(gameTime))
            {
                yield return command;
            }
        }
    }
}
