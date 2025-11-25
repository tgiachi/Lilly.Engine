using DryIoc;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data.Config;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Pipelines;
using Lilly.Engine.Services;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Renderers;
using Lilly.Rendering.Core.Services;

namespace Lilly.Engine.Bootstrap;

public class LillyBootstrap : ILillyBootstrap
{
    private readonly IContainer _container;

    private bool _isInitialized = false;

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
        RegisterServices();

        RegisterRenderLayers();
        OnConfiguring?.Invoke(_container);

        Renderer = new OpenGlRenderer(options.RenderConfig);
        Renderer.OnRender += RendererOnOnRender;
        Renderer.OnUpdate += RendererOnOnUpdate;
        Renderer.OnReady += RendererOnOnReady;
    }

    private void RendererOnOnReady(RenderContext context)
    {
        _container.RegisterInstance(context);

        _container.RegisterInstance(context.Renderer);
        _container.RegisterInstance(context.GraphicsDevice);
        _container.RegisterInstance(context.Input);
        _container.RegisterInstance(context.OpenGl);
        _container.RegisterInstance(context.Window);
        _container.RegisterInstance(context.DpiManager);
    }

    private void RegisterRenderLayers()
    {
        _container
            .RegisterRenderLayer<UpdateableLayer>()
            .RegisterRenderLayer<SpriteBatcherLayer>()
            ;
    }

    private void RendererOnOnUpdate(GameTime gameTime)
    {
        OnUpdate?.Invoke(gameTime);
    }

    private void RendererOnOnRender(GameTime gameTime)
    {
        if (!_isInitialized)
        {
            IntializeRenders();
            _isInitialized = true;
        }
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

    private void RegisterServices()
    {
        _container
            .RegisterService<IRenderPipeline, RenderPipeline>()
            .RegisterService<IAssetManager, AssetManager>()
            ;
    }

    private void IntializeRenders()
    {
        _container.Resolve<IRenderPipeline>();
    }
}
