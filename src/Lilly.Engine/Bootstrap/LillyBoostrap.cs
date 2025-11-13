using DryIoc;
using ImGuiNET;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Debuggers;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.Exceptions;
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
using Lilly.Engine.Wrappers.Debugger;
using Serilog;
using Silk.NET.Maths;

namespace Lilly.Engine.Bootstrap;

public class LillyBoostrap : ILillyBootstrap
{
    public IGraphicRenderer Renderer { get; }

    public event ILillyBootstrap.RenderHandler? OnRender;

    public event ILillyBootstrap.UpdateHandler? OnUpdate;

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

    /// <summary>
    /// Initializes the engine asynchronously with the provided options.
    /// </summary>
    /// <param name="options">The initial engine configuration options.</param>
    /// <returns>A task representing the initialization operation.</returns>
    public Task InitializeAsync(InitialEngineOptions options)
    {
        Renderer.Initialize(options);

        Renderer.Update += RendererOnUpdate;
        Renderer.Render += RendererOnRender;
        Renderer.Resize += RendererOnResize;

        _container.RegisterInstance(Renderer.Context);

        var pluginRegistry = _container.Resolve<PluginRegistry>();

        foreach (var plugin in pluginRegistry.GetLoadedPlugins())
        {
            plugin.EngineInitialized(_container);
        }

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
    public Task ShutdownAsync()
        => Task.CompletedTask;

    private void RegisterDefaults()
    {
        _container
            .Register<PluginRegistry>(Reuse.Singleton);

        _container
            .Register<PluginDependencyValidator>(Reuse.Singleton);

        _container
            .RegisterService<IScriptEngineService, LuaScriptEngineService>()
            .RegisterService<IVersionService, VersionService>()
            .RegisterService<IInputManagerService, InputManagerService>()
            .RegisterService<ITimerService, TimerService>()
            .RegisterService<IMainThreadDispatcher, MainThreadDispatcher>()
            .RegisterService<IJobSystemService, JobSystemService>()
            .RegisterService<IGraphicRenderPipeline, GraphicRenderPipeline>()
            .RegisterService<IGameObjectFactory, GameObjectFactory>()
            .RegisterService<IAssetManager, AssetManager>()
            .RegisterService<IPerformanceProfilerService, PerformanceProfilerService>()
            .RegisterService<ICamera3dService, Camera3dService>()
            .RegisterService<ISceneManager, SceneManager>()
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
            .AddScriptModule<ConsoleModule>()
            .AddScriptModule<WindowModule>()
            .AddScriptModule<AssetsModule>()
            .AddScriptModule<ImGuiModule>()
            .AddScriptModule<JobSystemModule>()
            .AddScriptModule<ScenesModule>()
            .AddScriptModule<InputManagerModule>()
            .AddScriptModule<CameraModule>()
            ;

    }

    private void InitializeRenderSystem()
    {
        var factory = _container.Resolve<IGameObjectFactory>();
        _renderPipeline = _container.Resolve<IGraphicRenderPipeline>();
        _container.RegisterInstance<IGameObjectManager>(_renderPipeline);
        _renderPipeline.Initialize();

        _renderPipeline.AddGameObject(_container.Resolve<IPerformanceProfilerService>());

        var gpuCommandRenderSystem = _renderPipeline.GetRenderLayerSystem<GpuCommandRenderSystem>();

        _renderPipeline.AddGameObject(new PerformanceDebugger(_container.Resolve<IPerformanceProfilerService>()));

        _renderPipeline.AddGameObject(
            new ImGuiActionDebugger(
                "Action Debugger",
                () =>
                {
                    var clearColor = gpuCommandRenderSystem.ClearColor.ToVector4();
                    ImGui.Text("Lilly Engine Action Debugger");
                    ImGui.ColorEdit4("Clear Color", ref clearColor);
                    gpuCommandRenderSystem.ClearColor = clearColor;
                }
            )
        );

        _renderPipeline.AddGameObject(factory.CreateGameObject<CameraDebugger>());

        _renderPipeline.AddGameObject(factory.CreateGameObject<InputDebugger>());

        // _renderPipeline.AddGameObject(
        //     new TextGameObject()
        //     {
        //         Text = "Lilly Lilly",
        //         Transform = { Position = new Vector2D<float>(300, 300) }
        //     }
        // );
        //
        // var logo = new ImageGameObject()
        // {
        //     TextureKey = "logo",
        // };
        //
        // logo.Transform.Position = new Vector2D<float>(200, 200);
        // logo.Transform.Scale = new Vector2D<float>(0.1f, 0.1f);
        //
        // var rectangle = new RectangleGameObject()
        // {
        //     Size = new Vector2D<float>(400, 100),
        //     Color = Color4b.CornflowerBlue,
        //     BorderThickness = 2,
        // };
        //
        // rectangle.Transform.Position = new Vector2D<float>(400, 400);
        // _renderPipeline.AddGameObject(rectangle);
        //
        // _renderPipeline.AddGameObject(logo);
        //
        // var textBox = new TextEditGameObject(
        //     _container.Resolve<IInputManagerService>(),
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // );
        //
        // textBox.Text = "Welcome to Lilly Engine!";
        // textBox.Transform.Position = new Vector2D<float>(50, 50);
        // textBox.Transform.Size = new Vector2D<float>(400, 32);
        //
        // _renderPipeline.AddGameObject(textBox);
        //
        // var button = new ButtonGameObject(
        //     _container.Resolve<IInputManagerService>(),
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // )
        // {
        //     Text = "Click Me!",
        //     Transform =
        //     {
        //         Position = new Vector2D<float>(50, 100),
        //         Size = new Vector2D<float>(150, 40)
        //     }
        // };
        //
        // button.Click += (sender, args) =>
        //                 {
        //                     _logger.Information("Button Clicked!");
        //                 };
        //
        // _renderPipeline.AddGameObject(button);
        //
        // var comboBox = new ComboBoxGameObject(
        //     _container.Resolve<IInputManagerService>(),
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // )
        // {
        //     Transform =
        //     {
        //         Position = new Vector2D<float>(50, 150),
        //         Size = new Vector2D<float>(200, 30)
        //     }
        // };
        //
        // comboBox.Items.Add("Option 1");
        // comboBox.Items.Add("Option 2");
        // comboBox.Items.Add("Option 3");
        // comboBox.SelectedIndex = 0;
        //
        // _renderPipeline.AddGameObject(comboBox);
        //
        // var progressBar = new ProgressBarGameObject(
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // )
        // {
        //     Transform =
        //     {
        //         Position = new Vector2D<float>(50, 200),
        //         Size = new Vector2D<float>(300, 25)
        //     },
        //     Progress = 0.5f
        // };
        //
        // _renderPipeline.AddGameObject(progressBar);
        //
        // var listBox = new ListBoxGameObject(
        //     _container.Resolve<IInputManagerService>(),
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // )
        // {
        //     Transform =
        //     {
        //         Position = new Vector2D<float>(50, 250),
        //         Size = new Vector2D<float>(200, 100)
        //     }
        // };
        //
        // listBox.Items.Add("Item 1");
        // listBox.Items.Add("Item 2");
        // listBox.Items.Add("Item 3");
        // listBox.Items.Add("Item 4");
        // listBox.Items.Add("Item 5");
        //
        // _renderPipeline.AddGameObject(listBox);
        //
        // var memo = new MemoEditGameObject(
        //     _container.Resolve<IInputManagerService>(),
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // )
        // {
        //     Transform =
        //     {
        //         Position = new Vector2D<float>(300, 50),
        //         Size = new Vector2D<float>(400, 150)
        //     },
        //     Text = "This is a memo edit box.\nYou can write multiple lines of text here."
        // };
        //
        // _renderPipeline.AddGameObject(memo);
        //
        // var checkBox = new CheckBoxGameObject(
        //     _container.Resolve<IInputManagerService>(),
        //     _container.Resolve<IAssetManager>(),
        //     UITheme.Default
        // )
        // {
        //     Transform =
        //     {
        //         Position = new Vector2D<float>(50, 370),
        //         Size = new Vector2D<float>(20, 20)
        //     },
        //     IsChecked = true,
        //     Label = "Is Checked",
        // };
        //
        // _renderPipeline.AddGameObject(checkBox);

        _renderPipeline.AddGameObject(new LogViewerDebugger(new LogViewer()));
        _renderPipeline.AddGameObject(new RenderPipelineDiagnosticsDebugger(_renderPipeline));
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

        _container.Resolve<ITimerService>();
        _container.Resolve<IMainThreadDispatcher>();

        _container.Resolve<IJobSystemService>()
                  .Initialize(Environment.ProcessorCount);

        var scriptEngine = _container.Resolve<IScriptEngineService>();

        InitializePlugins();

        _container.Resolve<IGameObjectFactory>();

        await scriptEngine.StartAsync();

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
