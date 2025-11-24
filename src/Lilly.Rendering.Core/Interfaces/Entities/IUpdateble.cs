using Lilly.Rendering.Core.Data.Game;

namespace Lilly.Rendering.Core.Interfaces.Entities;

public interface IUpdateble : IGameObject
{
    void Update(GameTime gameTime);
}
