using System.Numerics;
using DryIoc;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Data.Services;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Core.Interfaces.Services.Base;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Data.Config;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.GameObjects.TwoD;
using Lilly.Engine.GameObjects.ThreeD;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Interfaces.Scenes;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Lua.Scripting.Services;
using Lilly.Engine.Modules;
using Lilly.Engine.Pipelines;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Services;
using Lilly.Engine.Services.Input;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Extensions;
using Lilly.Rendering.Core.Interfaces.Renderers;
using Lilly.Rendering.Core.Interfaces.Services;
using Lilly.Rendering.Core.Renderers;
using Lilly.Rendering.Core.Services;
using Serilog;
using TrippyGL;

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

        RegisterGameObjects();

        OnConfiguring?.Invoke(_container);

        Renderer = new OpenGlRenderer(options.RenderConfig);
        Renderer.OnRender += RendererOnOnRender;
        Renderer.OnUpdate += RendererOnOnUpdate;
        Renderer.OnReady += RendererOnOnReady;
    }

    private void LoadDefaultAssets()
    {
        var assetManager = _container.Resolve<IAssetManager>();

        assetManager.LoadTextureFromMemory(
            "logo",
            ResourceUtils.GetEmbeddedResourceStream(typeof(LillyBootstrap).Assembly, "Assets/Textures/logo.png")
        );

        assetManager.LoadFontFromMemory(
            "default",
            ResourceUtils.GetEmbeddedResourceStream(typeof(LillyBootstrap).Assembly, "Assets/Fonts/DefaultMonoFont.ttf")
        );
        assetManager.LoadFontFromMemory(
            DefaultFonts.DefaultFontAlternateName,
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBootstrap).Assembly,
                "Assets/Fonts/DefaultMonoFontAlternative.ttf"
            )
        );

        assetManager.LoadFontFromMemory(
            DefaultFonts.DefaultMonocraftFontName,
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBootstrap).Assembly,
                "Assets/Fonts/Monocraft.ttf"
            )
        );

        assetManager.LoadFontFromMemory(
            DefaultFonts.DefaultFontHudName,
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBootstrap).Assembly,
                "Assets/Fonts/HornetDisplay-Regular.ttf"
            )
        );

        assetManager.LoadFontFromMemory(
            DefaultFonts.DefaultFontHudBoldName,
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBootstrap).Assembly,
                "Assets/Fonts/HornetDisplay-Bold.ttf"
            )
        );
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
            .RegisterRenderLayer<ThreeDLayer>()
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
            LoadDefaultAssets();
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

            var pipeline = _container.Resolve<IRenderPipeline>();

            var gameObjectFactory = _container.Resolve<IGameObjectFactory>();

            var versionGameObject = gameObjectFactory.Create<VersionGameObject>();

            _container.Resolve<IScriptEngineService>().ExecuteEngineReady();

            pipeline.AddGameObject(versionGameObject);


            var cube = gameObjectFactory.Create<SimpleCubeGameObject>();

            pipeline.AddGameObject(cube);

            // var text = new TextGameObject(_container.Resolve<IAssetManager>())
            // {
            //     Text = "Lilly Engine",
            //     FontSize = 48,
            //     Color = Color4b.Black,
            //     FontName = DefaultFonts.DefaultFontHudBoldName,
            //     Transform =
            //     {
            //         Position = new(200, 200)
            //     }
            // };

            // var fps = new FpsGameObject(_container.Resolve<IAssetManager>());
            // fps.Color = Color4b.Black;
            // fps.Transform.Position = new(100, 100);
            //
            // var rectagle = new RectangleGameObject()
            // {
            //     Size = new Vector2(200, 400),
            //     Color = Color4b.Aqua,
            //     Transform = { Position = new Vector2(400, 500) }
            // };
            //
            // pipeline.AddGameObject(fps);
            //
            // pipeline.AddGameObject(text);
            //
            // pipeline.AddGameObject(rectagle);
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

    private void RegisterGameObjects()
    {
        _container
            .RegisterGameObject<TextGameObject>()
            .RegisterGameObject<RectangleGameObject>()
            .RegisterGameObject<FpsGameObject>()
            .RegisterGameObject<TextureGameObject>()
            .RegisterGameObject<StackLayoutGameObject>()
            .RegisterGameObject<VersionGameObject>()
            .RegisterGameObject<SimpleCubeGameObject>()
            ;
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
            .RegisterService<ISceneManager, SceneManager>()
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
            .RegisterScriptModule<ScenesModule>()
            .RegisterScriptModule<CommandsModule>()
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
            _logger.Debug("Initializing {Name}", service.ServiceType.Name);
            _container.Resolve(service.ServiceType);
        }
    }

    private async Task StartServicesAsync()
    {
        var services = _container.Resolve<List<AutostartRegistration>>();

        foreach (var service in services)
        {
            _logger.Debug("Starting {Name}", service.ServiceType.Name);

            if (_container.Resolve(service.ServiceType) is ILillyService instance)
            {
                _logger.Debug("Starting {Name}", instance.GetType().Name);
                await instance.StartAsync();
            }
        }
    }
}
