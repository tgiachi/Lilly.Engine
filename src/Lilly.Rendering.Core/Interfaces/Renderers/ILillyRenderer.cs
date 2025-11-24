using Lilly.Rendering.Core.Data.Game;

namespace Lilly.Rendering.Core.Interfaces.Renderers;

public interface ILillyRenderer
{
    delegate void RenderDelegate(GameTime gameTime);

    delegate void UpdateDelegate(GameTime gameTime);

    delegate void ReadyDelegate();

    delegate void ResizeDelegate(int width, int height);

    event RenderDelegate? OnRender;
    event UpdateDelegate? OnUpdate;
    event ResizeDelegate? OnResize;

    event ReadyDelegate? OnReady;
}
