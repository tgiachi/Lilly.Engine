using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

public interface IGameObject
{
    IGameObject? Parent { get; set; }

    GameObjectCollection<IGameObject> Children { get; }

    uint Id { get; set; }

    string Name { get; set; }

    ushort Order { get; }

    /// <summary>
    /// Renders the game object and returns an enumerable collection of render commands.
    /// This approach ensures immutability and prevents accidental modification of other objects' commands.
    /// </summary>
    /// <param name="gameTime">The current game time.</param>
    /// <returns>An enumerable collection of render commands to be processed by the rendering pipeline.</returns>
    IEnumerable<RenderCommand> Render(GameTime gameTime);
}
