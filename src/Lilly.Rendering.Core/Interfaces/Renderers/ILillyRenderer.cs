using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Data.Game;

namespace Lilly.Rendering.Core.Interfaces.Renderers;

public interface ILillyRenderer
{
    delegate void RenderDelegate(GameTime gameTime);
    delegate void UpdateDelegate(GameTime gameTime);
    delegate void ReadyDelegate(RenderContext context);
    delegate void ResizeDelegate(int width, int height);
    delegate void ClosingDelegate();


    void Run();
    event RenderDelegate? OnRender;
    event UpdateDelegate? OnUpdate;
    event ResizeDelegate? OnResize;
    event ReadyDelegate? OnReady;

    event ClosingDelegate? OnClosing;

}
