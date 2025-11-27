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
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Debuggers;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.Exceptions;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects.TwoD;
using Lilly.Engine.GameObjects.ThreeD;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Lua.Scripting.Services;
using Lilly.Engine.Modules;
using Lilly.Engine.Pipelines;
using Lilly.Engine.Plugins;
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

        InitializePlugins();

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

            InizializeGameObjectFromPlugins();

            _isServiceStarted = true;

            var pipeline = _container.Resolve<IRenderPipeline>();

            var gameObjectFactory = _container.Resolve<IGameObjectFactory>();

            var versionGameObject = gameObjectFactory.Create<VersionGameObject>();

            pipeline.AddGameObject(gameObjectFactory.Create<RenderPipelineDebugger>());
            pipeline.AddGameObject(gameObjectFactory.Create<PerformanceDebugger>());
            _container.Resolve<IScriptEngineService>().ExecuteEngineReady();

            pipeline.AddGameObject(versionGameObject);

            foreach (var index in Enumerable.Range(0, 3))
            {
                var cube = gameObjectFactory.Create<SimpleCubeGameObject>();

                cube.Transform.Position = new(index);
                pipeline.AddGameObject(cube);
            }
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

        _container
            .RegisterGameObject<RenderPipelineDebugger>()
            .RegisterGameObject<PerformanceDebugger>();
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
            .RegisterService<IPerformanceProfilerService, PerformanceProfilerService>()
            .RegisterService<PluginRegistry>()
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
            .RegisterScriptModule<Rendering3dModule>()
            .RegisterScriptModule<ImGuiModule>()
            ;

        _container.RegisterLuaUserData<GameTime>();
    }

    private void IntializeRenders()
    {
        var renderPipeLine = _container.Resolve<IRenderPipeline>();
        _container.RegisterInstance<IGameObjectManager>(renderPipeLine);
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

    private void InizializeGameObjectFromPlugins()
    {
        var pluginRegistry = _container.Resolve<PluginRegistry>();
        var loadedPlugins = pluginRegistry.GetLoadedPlugins().ToList();
        var renderPipeline = _container.Resolve<IRenderPipeline>();

        foreach (var plugin in loadedPlugins)
        {
            _logger.Debug("Loading global game objects from plugin {PluginId}", plugin.LillyData.Id);

            plugin.EngineReady(_container);

            foreach (var globalGameObject in plugin.GetGlobalGameObjects(_container.Resolve<IGameObjectFactory>()))
            {
                _logger.Debug(
                    "Adding game object {GameObjectType} from plugin {PluginId}",
                    globalGameObject.GetType().Name,
                    plugin.LillyData.Id
                );

                renderPipeline.AddGameObject(globalGameObject);
            }
        }
    }

    private void InitializePlugins()
    {
        var pluginRegistry = _container.Resolve<PluginRegistry>();

        var pluginRegistrations = _container.Resolve<List<EnginePluginRegistration>>();

        _logger.Debug("Discovering plugins...");

        var allPluginData = pluginRegistrations
                            .Select(pluginRegistration => (ILillyPlugin)_container.Resolve(pluginRegistration.PluginType))
                            .Select(plugin => plugin.LillyData)
                            .ToList();

        _logger.Information("Discovered {PluginCount} plugins", allPluginData.Count);

        _logger.Debug("Checking for circular dependencies...");
        var circularDependencies = pluginRegistry.CheckForCircularDependencies(allPluginData);

        if (circularDependencies.Count != 0)
        {
            var cycle = string.Join(" -> ", circularDependencies);
            _logger.Fatal("Circular dependency detected: {Cycle}", cycle);

            throw new PluginLoadException(
                $"Circular dependency detected in plugin chain: {cycle}",
                "circular-dependency",
                allPluginData.First(),
                allPluginData
            );
        }

        _logger.Debug("Loading plugins in dependency order...");
        var sortedPluginData = pluginRegistry.GetPluginsInDependencyOrder(allPluginData).ToList();

        foreach (var pluginData in sortedPluginData)
        {
            var pluginRegistration = pluginRegistrations.First(
                pr =>
                {
                    var plugin = (ILillyPlugin)_container.Resolve(pr.PluginType);

                    return plugin.LillyData.Id == pluginData.Id;
                }
            );

            try
            {
                _logger.Debug("Loading plugin from assembly {Assembly}", pluginRegistration.Assembly.FullName);

                var plugin = (ILillyPlugin)_container.Resolve(pluginRegistration.PluginType);

                _logger.Information(
                    "Registering plugin {PluginId} v{Version} by {Author}",
                    plugin.LillyData.Id,
                    plugin.LillyData.Version,
                    plugin.LillyData.Author
                );

                pluginRegistry.RegisterPlugin(plugin);

                _logger.Debug("Registering modules for plugin {PluginId}", plugin.LillyData.Id);
                plugin.RegisterModule(_container);
            }
            catch (PluginLoadException ex)
            {
                _logger.Fatal(ex, "Failed to load plugin {PluginId}", pluginData.Id);

                throw;
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Unexpected error loading plugin {PluginId}", pluginData.Id);

                throw new PluginLoadException(
                    $"Unexpected error loading plugin '{pluginData.Id}': {ex.Message}",
                    pluginData.Id,
                    pluginData,
                    pluginRegistry.GetLoadedPluginData(),
                    ex
                );
            }
        }
    }
}
