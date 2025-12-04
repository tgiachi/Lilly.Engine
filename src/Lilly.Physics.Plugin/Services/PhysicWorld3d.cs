using BepuPhysics;
using BepuUtilities;
using BepuUtilities.Memory;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Interfaces.Services;
using Lilly.Physics.Plugin.Callbacks;
using Lilly.Physics.Plugin.Data;
using Lilly.Rendering.Core.Context;
using Serilog;

namespace Lilly.Physics.Plugin.Services;

public class PhysicWorld3d : IPhysicWorld3d, IDisposable
{
    private readonly World3dPhysicConfig _config;

    private readonly ILogger _logger = Log.ForContext<PhysicWorld3d>();
    public BufferPool Pool { get; }

    public ThreadDispatcher ThreadDispatcher { get; }

    public Simulation Simulation { get; private set; }

    public PhysicWorld3d(World3dPhysicConfig config, RenderContext renderContext)
    {
        _config = config;
        Pool = new BufferPool();
        ThreadDispatcher = new ThreadDispatcher(config.ThreadCount);
        renderContext.Renderer.OnUpdate += Update;

        _logger.Information("Starting Physic World3d with {ThreadCount} threads", config.ThreadCount);
    }

    private void Update(GameTime gameTime)
    {
        Simulation.Timestep(gameTime.GetTotalGameTimeSeconds(), ThreadDispatcher);
    }

    public void Dispose()
    {
        Simulation.Dispose();
        Pool.Clear();

        GC.SuppressFinalize(this);
    }

    public async Task StartAsync()
    {
        _logger.Information("Initializing Physic World3d simulation with gravity {Gravity}", _config.Gravity);
        Simulation = Simulation.Create(
            Pool,
            new DefaultNarrowPhaseCallbacks(),
            new DefaultPoseIntegratorCallbacks(_config.Gravity),
            new SolveDescription(8, 1)
        );
    }

    public Task ShutdownAsync()
    {
        return Task.CompletedTask;
    }
}
