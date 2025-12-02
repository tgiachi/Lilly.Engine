using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Context;

namespace Lilly.Rendering.Core.Interfaces.Renderers;

public interface IGraphicRenderer
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
