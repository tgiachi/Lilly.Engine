using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Data.Config;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Lilly.Rendering.Core.Renderers;

namespace Lilly.Engine.Bootstrap;

public class LillyBootstrap : ILillyBootstrap

{
    private readonly IContainer _container;

    public IGraphicRenderer Renderer { get; private set; }
    public event IGraphicRenderer.RenderDelegate? OnRender;
    public event IGraphicRenderer.UpdateDelegate? OnUpdate;
    public event ILillyBootstrap.ConfiguringHandler? OnConfiguring;

    public LillyBootstrap(IContainer container)
    {
        _container = container;
    }

    public async Task InitializeAsync(InitialEngineOptions options)
    {
        OnConfiguring?.Invoke(_container);
        Renderer = new OpenGlRenderer(options.RenderConfig);
        Renderer.OnRender += RendererOnOnRender;
        Renderer.OnUpdate += RendererOnOnUpdate;
    }

    private void RendererOnOnUpdate(GameTime gameTime)
    {
        OnUpdate?.Invoke(gameTime);
    }

    private void RendererOnOnRender(GameTime gameTime)
    {
        OnRender?.Invoke(gameTime);
    }

    public async Task RunAsync()
    {
        Renderer.Run();
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }
}
