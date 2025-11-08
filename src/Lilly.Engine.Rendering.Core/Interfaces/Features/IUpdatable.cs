using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Features;

public interface IUpdatable : IGameObject
{
    void Update(GameTime gameTime);
}
