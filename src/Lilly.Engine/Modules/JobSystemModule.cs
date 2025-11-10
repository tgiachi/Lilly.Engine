using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Core.Interfaces.Dispatchers;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Wrappers;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Modules;

[ScriptModule("job_system")]
/// <summary>
/// Provides scripting access to the job system for scheduling tasks and running code on the main thread.
/// </summary>
public class JobSystemModule
{
    private readonly IJobSystemService _jobSystemService;

    private readonly IMainThreadDispatcher _mainThreadDispatcher;

    public JobSystemModule(IJobSystemService jobSystemService, IMainThreadDispatcher mainThreadDispatcher)
    {
        _jobSystemService = jobSystemService;
        _mainThreadDispatcher = mainThreadDispatcher;
    }

    [ScriptFunction("run_in_main_thread", "Schedules a closure to be executed in the main thread.")]
    /// <summary>
    /// Schedules a Lua closure to be executed in the main thread.
    /// </summary>
    /// <param name="closure">The Lua closure to execute.</param>
    public void RunInMainThread(Closure closure)
    {
        _mainThreadDispatcher.EnqueueAction(() => { closure.Call(); });
    }

    [ScriptFunction("schedule", "Schedules a job to be executed by the job system.")]
    /// <summary>
    /// Schedules a job to be executed by the job system.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="closure">The Lua closure to execute.</param>
    /// <param name="userData">Optional user data to pass to the job.</param>
    public void Schedule(string name, Closure closure, object? userData = null)
    {
        _jobSystemService.Schedule(new LuaJobWrap(name, closure, userData));
    }
}
