using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

public interface IGameObject
{
    IGameObject? Parent { get; set; }

    GameObjectCollection<IGameObject> Children { get; }

    uint Id { get; set; }

    string Name { get; }

    ushort Order { get; }

    void Render(GameTime gameTime, ref List<RenderCommand> renderCommands);
}
