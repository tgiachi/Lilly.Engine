using DryIoc;
using ImGuiNET;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Debuggers;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Layers;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Lua.Scripting.Services;
using Lilly.Engine.Modules;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Services;
using Lilly.Engine.Services;
using Lilly.Engine.Wrappers;
using Serilog;
using Silk.NET.Maths;
using TrippyGL;

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
            ;

        _container
            .RegisterRenderSystem<GpuCommandRenderSystem>()
            .RegisterRenderSystem<ImGuiRenderSystem>()
            .RegisterRenderSystem<SpriteBatchRenderSystem>()
            .RegisterRenderSystem<UpdatableRenderSystem>()
            .RegisterRenderSystem<InputRenderSystem>()
            .RegisterRenderSystem<RenderLayerSystem3D>()
            ;

        _container.RegisterGameObject<ImGuiActionDebugger>();

        _container.AddLuaUserData<Vector2D<int>>();
        _container
            .AddScriptModule<ConsoleModule>()
            .AddScriptModule<WindowModule>()
            .AddScriptModule<AssetsModule>()
            .AddScriptModule<ImGuiModule>()
            .AddScriptModule<JobSystemModule>()
            .AddScriptModule<InputManagerModule>()
            ;
    }

    private void InitializeRenderSystem()
    {
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

        _renderPipeline.AddGameObject(
            new TextGameObject()
            {
                Text = "Lilly Lilly",
                Transform = { Position = new Vector2D<float>(300, 300) }
            }
        );

        var logo = new ImageGameObject()
        {
            TextureKey = "logo",
        };

        logo.Transform.Position = new Vector2D<float>(200, 200);
        logo.Transform.Scale = new Vector2D<float>(0.1f, 0.1f);

        var rectangle = new RectangleGameObject()
        {
            Size = new Vector2D<float>(400, 100),
            Color = Color4b.CornflowerBlue,
            BorderThickness = 2,
        };

        rectangle.Transform.Position = new Vector2D<float>(400, 400);
        _renderPipeline.AddGameObject(rectangle);

        _renderPipeline.AddGameObject(logo);

        var textBox = new TextEditGameObject(
            _container.Resolve<IInputManagerService>(),
            _container.Resolve<IAssetManager>(),
            UITheme.Default
        );

        textBox.Text = "Welcome to Lilly Engine!";
        textBox.Transform.Position = new Vector2D<float>(50, 50);
        textBox.Transform.Size = new Vector2D<float>(400, 32);

        _renderPipeline.AddGameObject(textBox);

        var button = new ButtonGameObject(
            _container.Resolve<IInputManagerService>(),
            _container.Resolve<IAssetManager>(),
            UITheme.Default
        )
        {
            Text = "Click Me!",
            Transform =
            {
                Position = new Vector2D<float>(50, 100),
                Size = new Vector2D<float>(150, 40)
            }
        };

        button.Click += (sender, args) =>
                        {
                            _logger.Information("Button Clicked!");
                        };

        _renderPipeline.AddGameObject(button);


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
