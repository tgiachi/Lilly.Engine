
using Lilly.Engine.Core.Data.Privimitives;

namespace Lilly.Rendering.Core.Interfaces.Entities;

public interface IUpdateble : IGameObject
{
    void Update(GameTime gameTime);
}
