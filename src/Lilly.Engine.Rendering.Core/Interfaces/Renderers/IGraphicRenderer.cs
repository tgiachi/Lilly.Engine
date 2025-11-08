using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Interfaces.Renderers;

public interface IGraphicRenderer
{

    delegate void RenderHandler(GameTime gameTime);

    delegate void UpdateHandler(GameTime gameTime);

    delegate void ResizeHandler(int width, int height);

    string Name { get; }

    RendererType RendererType { get; }


    RenderContext Context { get; }

    void Initialize(InitialEngineOptions options);


    void Run();


    event UpdateHandler Update;

    event RenderHandler Render;

    event ResizeHandler Resize;




}
