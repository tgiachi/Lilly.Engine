using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;

namespace Lilly.Engine.Interfaces.Bootstrap;

public interface ILillyBootstrap
{

    delegate void RenderHandler(GameTime gameTime);

    delegate void UpdateHandler(GameTime gameTime);

    IGraphicRenderer Renderer { get; }

    event RenderHandler OnRender;

    event UpdateHandler OnUpdate;

    Task InitializeAsync(InitialEngineOptions options);

    Task RunAsync();

    Task ShutdownAsync();
}
