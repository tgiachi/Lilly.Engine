using DryIoc;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Data.Services;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Core.Interfaces.Services.Base;
using Lilly.Engine.Data.Config;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Lua.Scripting.Services;
using Lilly.Engine.Modules;
using Lilly.Engine.Pipelines;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Services;
using Lilly.Engine.Services.Input;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Renderers;
using Lilly.Rendering.Core.Services;
using Serilog;

namespace Lilly.Engine.Bootstrap;

public class LillyBootstrap : ILillyBootstrap
{
    private readonly IContainer _container;

    private readonly ILogger _logger = Log.ForContext<LillyBootstrap>();

    private bool _isInitialized;
    private bool _isServiceInitialized;
    private bool _isServiceStarted;

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
        var directoriesConfig = _container.Resolve<DirectoriesConfig>();

        _logger.Information("Root Directory: {RootDirectory}", directoriesConfig.Root);

        RegisterServices();
        RegisterScriptModules();

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
            .RegisterRenderLayer<InputLayer>()
            .RegisterRenderLayer<ImGuiRenderSystem>()
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

        if (!_isServiceInitialized)
        {
            InitializeServicesAsync().GetAwaiter().GetResult();
            _isServiceInitialized = true;
        }

        if (!_isServiceStarted)
        {
            StartServicesAsync().GetAwaiter().GetResult();
            _isServiceStarted = true;
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
            .RegisterService<IEventBusService, EventBusService>()
            .RegisterService<IMainThreadDispatcher, MainThreadDispatcher>()
            .RegisterService<INotificationService, NotificationService>()
            .RegisterService<IRenderPipeline, RenderPipeline>()
            .RegisterService<IAssetManager, AssetManager>()
            .RegisterService<IInputManagerService, InputManagerService>()
            .RegisterService<IVersionService, VersionService>()
            .RegisterService<IJobSystemService, JobSystemService>()
            .RegisterService<ICommandSystemService, CommandSystemService>()
            .RegisterService<IAudioService, AudioService>()
            .RegisterService<ITimerService, TimerService>()
            .RegisterService<ICamera3dService, Camera3dService>()
            .RegisterService<IGameObjectFactory, GameObjectFactory>()
            .RegisterService<IScriptEngineService, LuaScriptEngineService>(true)
            ;

        _container.RegisterInstance(new JobServiceConfig(Environment.ProcessorCount - 1));
    }

    private void RegisterScriptModules()
    {
        _container
            .RegisterScriptModule<ConsoleModule>()
            .RegisterScriptModule<AssetsModule>()
            .RegisterScriptModule<EngineModule>()
            .RegisterScriptModule<NotificationsModule>()
            .RegisterScriptModule<JobSystemModule>()
            .RegisterScriptModule<InputManagerModule>()
            .RegisterScriptModule<WindowModule>()
            .RegisterScriptModule<CameraModule>()
            .RegisterScriptModule<ImGuiModule>()
            ;

        _container.RegisterLuaUserData<GameTime>();
    }

    private void IntializeRenders()
    {
        _container.Resolve<IRenderPipeline>();
    }

    private async Task InitializeServicesAsync()
    {
        var services = _container.Resolve<List<AutostartRegistration>>();

        foreach (var service in services)
        {
            _logger.Debug("Initializing {Name}", service.GetType().Name);
            _container.Resolve(service.ServiceType);
        }
    }

    private async Task StartServicesAsync()
    {
        var services = _container.Resolve<List<AutostartRegistration>>();

        foreach (var service in services)
        {
            _logger.Debug("Starting {Name}", service.GetType().Name);

            if (_container.Resolve(service.ServiceType) is ILillyService instance)
            {
                _logger.Debug("Starting {Name}", instance.GetType().Name);
                await instance.StartAsync();
            }
        }
    }
}
