using DryIoc;
using ImGuiNET;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Data;
using Lilly.Engine.Dispatchers;
using Lilly.Engine.Extensions;
using Lilly.Engine.Interfaces.Bootstrap;
using Lilly.Engine.Layers;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Engine.Lua.Scripting.Services;
using Lilly.Engine.Modules;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Data.Config;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.Renderers;
using Lilly.Engine.Rendering.Core.Services;
using Lilly.Engine.Services;
using Lilly.Engine.Wrappers;
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

    public LillyBoostrap(IContainer container, IGraphicRenderer renderer)
    {
        _container = container;
        Renderer = renderer;

        container.RegisterInstance(renderer);

        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        _container
            .RegisterService<IScriptEngineService, LuaScriptEngineService>()
            .RegisterService<IVersionService, VersionService>()
            .RegisterService<ITimerService, TimerService>()
            .RegisterService<IMainThreadDispatcher, MainThreadDispatcher>()
            .RegisterService<IJobSystemService, JobSystemService>()
            .RegisterService<IGraphicRenderPipeline, GraphicRenderPipeline>()
            ;

        _container
            .RegisterRenderSystem<ImGuiRenderSystem>()
            .RegisterRenderSystem<SpriteBatchRenderSystem>()
            .RegisterRenderSystem<UpdatableRenderSystem>()
            ;

        _container.AddLuaUserData<Vector2D<int>>();
        _container.AddScriptModule<ConsoleModule>();
    }

    public Task InitializeAsync(InitialEngineOptions options)
    {
        Renderer.Initialize(options);

        Renderer.Update += RendererOnUpdate;
        Renderer.Render += RendererOnRender;
        Renderer.Resize += RendererOnResize;

        _container.RegisterInstance(Renderer.Context);

        return Task.CompletedTask;
    }

    private void RendererOnResize(int width, int height)
    {
        _renderPipeline.ViewportResize(width, height);
    }

    private void RendererOnRender(GameTime gameTime)
    {
        if (!_isRenderInitialized)
        {
            _logger.Information("Render System Initialized Successfully.");

            _container.Resolve<IScriptEngineService>().ExecuteEngineReady();
            _isRenderInitialized = true;
        }
        OnRender?.Invoke(gameTime);
        _renderPipeline.Render(gameTime);
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

        await scriptEngine.StartAsync();

        _renderPipeline = _container.Resolve<IGraphicRenderPipeline>();
        _renderPipeline.Initialize();

        _renderPipeline.AddGameObject(
            new ImGuiActionDebugger(
                "Action Debugger",
                () =>
                {
                    var clearColor = Renderer.Context.GraphicsDevice.ClearColor;
                    ImGui.Text("Lilly Engine Action Debugger");
                    ImGui.ColorEdit4("Clear Color", ref clearColor);
                    Renderer.Context.GraphicsDevice.ClearColor = clearColor;
                }
            )
        );
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
        _renderPipeline.Update(gameTime);
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
