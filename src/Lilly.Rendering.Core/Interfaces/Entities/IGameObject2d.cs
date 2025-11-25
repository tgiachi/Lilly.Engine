using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.SpriteBatcher;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Rendering.Core.Interfaces.Entities;

public interface IGameObject2d : IGameObject
{
    Transform2D Transform { get; }

    void Draw(GameTime gameTime, ILillySpriteBatcher spriteBatcher);
}
