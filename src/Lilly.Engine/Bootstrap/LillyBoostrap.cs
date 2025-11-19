using DryIoc;
using ImGuiNET;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Data.Services;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Core.Interfaces.Services.Base;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Debuggers;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.Exceptions;
using Lilly.Engine.GameObjects;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Layers;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Lua.Scripting.Services;
using Lilly.Engine.Modules;
using Lilly.Engine.Plugins;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Services;
using Lilly.Engine.Services;
using Lilly.Engine.Services.Input;
using Lilly.Engine.Wrappers.Commands;
using Lilly.Engine.Wrappers.Debugger;
using MoonSharp.Interpreter;
using Serilog;
using Silk.NET.Maths;

namespace Lilly.Engine.Bootstrap;

public class LillyBoostrap : ILillyBootstrap
{
    public IGraphicRenderer Renderer { get; }

    public event ILillyBootstrap.RenderHandler? OnRender;

    public event ILillyBootstrap.UpdateHandler? OnUpdate;
    public event ILillyBootstrap.ConfiguringHandler? OnConfiguring;

    private readonly IContainer _container;
    private IGraphicRenderPipeline _renderPipeline;

    private readonly ILogger _logger = Log.ForContext<LillyBoostrap>();

    private bool _initialized;
    private bool _isRenderInitialized;

    /// <summary>
    /// Initializes a new instance of the LillyBoostrap class.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    /// <param name="renderer">The graphics renderer.</param>
    public LillyBoostrap(IContainer container, IGraphicRenderer renderer)
    {
        _container = container;
        Renderer = renderer;

        container.RegisterInstance(renderer);

        RegisterDefaults();
    }

    /// <summary>
    /// Initializes the engine asynchronously with the provided options.
    /// </summary>
    /// <param name="options">The initial engine configuration options.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public Task InitializeAsync(InitialEngineOptions options)
    {
        OnConfiguring?.Invoke(_container);

        Renderer.Initialize(options);

        Renderer.Update += RendererOnUpdate;
        Renderer.Render += RendererOnRender;
        Renderer.Resize += RendererOnResize;

        _container.RegisterInstance(Renderer.Context);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Runs the engine's main loop asynchronously.
    /// </summary>
    /// <returns>A task representing the run operation.</returns>
    public async Task RunAsync()
        => Renderer.Run();

    /// <summary>
    /// Shuts down the engine asynchronously.
    /// </summary>
    /// <returns>A task representing the shutdown operation.</returns>
    public async Task ShutdownAsync()
    {
        _logger.Information("Shutting down Lilly Engine...");

        try
        {
            // Shutdown job system first to stop background workers
            var jobSystem = _container.Resolve<IJobSystemService>();
            jobSystem?.Shutdown();
            _logger.Debug("Job system shut down");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error shutting down job system");
        }

        try
        {
            // Dispose audio service
            var audioService = _container.Resolve<IAudioService>();
            audioService?.Dispose();
            _logger.Debug("Audio service disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing audio service");
        }

        try
        {
            // Dispose event bus service (handles pending events)
            var eventBus = _container.Resolve<IEventBusService>();
            (eventBus as IDisposable)?.Dispose();
            _logger.Debug("Event bus service disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing event bus service");
        }

        try
        {
            // Dispose timer service
            var timerService = _container.Resolve<ITimerService>();
            (timerService as IDisposable)?.Dispose();
            _logger.Debug("Timer service disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing timer service");
        }

        try
        {
            // Dispose asset manager BEFORE renderer to clean up GPU resources while context is active
            var assetManager = _container.Resolve<IAssetManager>();
            (assetManager as IDisposable)?.Dispose();
            _logger.Debug("Asset manager disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing asset manager");
        }

        try
        {
            // Dispose renderer LAST to close the graphics context
            (Renderer as IDisposable)?.Dispose();
            _logger.Debug("Renderer disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing renderer");
        }

        _logger.Information("Lilly Engine shutdown complete");
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

    private void InitializeRenderSystem()
    {
        _container.RegisterInstance(Renderer.Context.GraphicsDevice);

        _container.RegisterGameObject<SimpleCubeGameObject>();
        _container.RegisterGameObject<SimpleSphereGameObject>();
        _container.RegisterGameObject<SimplePyramidGameObject>();
        _container.RegisterGameObject<SimpleTorusGameObject>();
        _container.RegisterGameObject<SimpleStarGameObject>();
        _container.RegisterGameObject<SimpleFractalCubeGameObject>();

        _renderPipeline = _container.Resolve<IGraphicRenderPipeline>();
        _container.RegisterInstance<IGameObjectManager>(_renderPipeline);
        _renderPipeline.Initialize();

        InizializeGameObjectFromPlugins();

        //generate 10 random cubes for testing
        var random = new Random();
        var gameObjectFactory = _container.Resolve<IGameObjectFactory>();

        for (int i = 0; i < 10; i++)
        {
            var cube = gameObjectFactory.Create<SimpleCubeGameObject>();
            cube.Transform.Position = new Vector3D<float>(
                random.Next(-10, 10),
                random.Next(-10, 10),
                random.Next(-10, 10)
            );
            cube.Transform.Scale = new Vector3D<float>(
                random.Next(1, 3),
                random.Next(1, 3),
                random.Next(1, 3)
            );
            _renderPipeline.AddGameObject(cube);
        }
    }

    private void InizializeGameObjectFromPlugins()
    {
        var pluginRegistry = _container.Resolve<PluginRegistry>();
        var loadedPlugins = pluginRegistry.GetLoadedPlugins().ToList();

        _logger.Information("Loading game objects from {PluginCount} plugins", loadedPlugins.Count);

        foreach (var plugin in loadedPlugins)
        {
            _logger.Debug("Loading global game objects from plugin {PluginId}", plugin.LillyData.Id);

            foreach (var globalGameObject in plugin.GlobalGameObjects(_container.Resolve<IGameObjectFactory>()))
            {
                _logger.Debug(
                    "Adding game object {GameObjectType} from plugin {PluginId}",
                    globalGameObject.GetType().Name,
                    plugin.LillyData.Id
                );

                _renderPipeline.AddGameObject(globalGameObject);
            }
        }
    }

    private void RegisterDefaults()
    {
        _container.RegisterInstance(new JobServiceConfig(Environment.ProcessorCount));

        _container
            .Register<PluginRegistry>(Reuse.Singleton);

        _container
            .Register<PluginDependencyValidator>(Reuse.Singleton);

        _container
            .RegisterService<IEventBusService, EventBusService>(true)
            .RegisterService<ITimerService, TimerService>(true)
            .RegisterService<IMainThreadDispatcher, MainThreadDispatcher>(true)
            .RegisterService<IJobSystemService, JobSystemService>(true)
            .RegisterService<IScriptEngineService, LuaScriptEngineService>(true)
            .RegisterService<IVersionService, VersionService>()
            .RegisterService<IInputManagerService, InputManagerService>()
            .RegisterService<IGraphicRenderPipeline, GraphicRenderPipeline>()
            .RegisterService<IGameObjectFactory, GameObjectFactory>()
            .RegisterService<IAssetManager, AssetManager>()
            .RegisterService<IPerformanceProfilerService, PerformanceProfilerService>()
            .RegisterService<ICamera3dService, Camera3dService>()
            .RegisterService<ISceneManager, SceneManager>()
            .RegisterService<INotificationService, NotificationService>()
            .RegisterService<IAudioService, AudioService>(true)
            .RegisterService<ICommandSystemService, CommandSystemService>(true)
            ;

        _container
            .RegisterRenderSystem<GpuCommandRenderSystem>()
            .RegisterRenderSystem<ImGuiRenderSystem>()
            .RegisterRenderSystem<SpriteBatchRenderSystem>()
            .RegisterRenderSystem<UpdatableRenderSystem>()
            .RegisterRenderSystem<InputRenderSystem>()
            .RegisterRenderSystem<RenderLayerSystem3D>()
            ;

        _container
            .RegisterGameObject<ImGuiActionDebugger>()
            .RegisterGameObject<CameraDebugger>()
            .RegisterGameObject<InputDebugger>()
            ;

        _container.AddLuaUserData<Vector2D<int>>();

        _container
            .AddScriptModule<EngineModule>()
            .AddScriptModule<CommandsModule>()
            .AddScriptModule<ConsoleModule>()
            .AddScriptModule<WindowModule>()
            .AddScriptModule<AssetsModule>()
            .AddScriptModule<ImGuiModule>()
            .AddScriptModule<JobSystemModule>()
            .AddScriptModule<ScenesModule>()
            .AddScriptModule<InputManagerModule>()
            .AddScriptModule<CameraModule>()
            .AddScriptModule<NotificationsModule>()
            ;

        UserData.RegisterType<GameTime>();
    }

    private void RendererOnRender(GameTime gameTime)
    {
        if (!_isRenderInitialized)
        {
            _logger.Information("Render System Initialized Successfully.");
            Renderer.Context.GraphicsDevice.SetViewport(
                0,
                0,
                (uint)Renderer.Context.Window.Size.X,
                (uint)Renderer.Context.Window.Size.Y
            );
            InitializeRenderSystem();
            _container.Resolve<IScriptEngineService>().ExecuteEngineReady();

            var sceneManager = _container.Resolve<ISceneManager>();

            _renderPipeline.AddGameObject(sceneManager);

            _isRenderInitialized = true;
        }
        OnRender?.Invoke(gameTime);
        _renderPipeline.Render(gameTime);
    }

    private void RendererOnResize(int width, int height)
    {
        _renderPipeline.ViewportResize(width, height);
    }

    private void RendererOnUpdate(GameTime gameTime)
    {
        if (!_initialized)
        {
            _logger.Information("Lilly Engine Initialized Successfully.");
            StartServicesAsync().GetAwaiter().GetResult();
            _initialized = true;
        }
        OnUpdate?.Invoke(gameTime);

        _renderPipeline?.Update(gameTime);
    }

    private async Task StartServicesAsync()
    {
        var directoriesConfig = _container.Resolve<DirectoriesConfig>();

        _logger.Information("Root Directory: {RootDirectory}", directoriesConfig.Root);

        var autostartServices = _container.Resolve<List<AutostartRegistration>>();

        foreach (var autostart in autostartServices)
        {
            _container.Resolve(autostart.ServiceType);
            _logger.Debug("Autostarting service: {ServiceType}", autostart.ServiceType.Name);
        }



        InitializePlugins();

        var pluginRegistry = _container.Resolve<PluginRegistry>();

        foreach (var plugin in pluginRegistry.GetLoadedPlugins())
        {
            plugin.EngineInitialized(_container);
        }

        _container.Resolve<IGameObjectFactory>();

        foreach (var service in autostartServices)
        {
            if (_container.Resolve(service.ServiceType) is ILillyService instance)
            {
                await instance.StartAsync();
                _logger.Debug("Started autostart service: {ServiceType}", service.ServiceType.Name);
            }
        }


        var assetManager = _container.Resolve<IAssetManager>();

        assetManager.LoadFontFromMemory(
            "defaultUiFont",
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBoostrap).Assembly,
                "Assets.Fonts.DefaultMonoFont.ttf"
            )
        );

        assetManager.LoadFontFromMemory(
            "defaultGameFont",
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBoostrap).Assembly,
                "Assets.Fonts.Monocraft.ttf"
            )
        );

        assetManager.LoadFontFromMemory(
            "imGuiFontAlternative",
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBoostrap).Assembly,
                "Assets.Fonts.ImGuiFontAlternative.ttf"
            )
        );

        assetManager.LoadFontFromMemory(
            "defaultUiAlterativeFont",
            ResourceUtils.GetEmbeddedResourceStream(
                typeof(LillyBoostrap).Assembly,
                "Assets.Fonts.DefaultMonoFontAlternative.ttf"
            )
        );

        assetManager.LoadTextureFromMemory(
            "logo",
            ResourceUtils.GetEmbeddedResourceStream(typeof(LillyBoostrap).Assembly, "Assets.Textures.SquidVortexLogo.png")
        );
    }
}
