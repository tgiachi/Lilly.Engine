using Lilly.Engine.Core.Interfaces.Jobs;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Wrappers.Jobs;

/// <summary>
/// Wraps a Lua closure to be executed as a job in the job system.
/// </summary>
public class LuaJobWrap : IJob
{
    private readonly Closure _execute;

    public object? State { get; set; }

    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the LuaJobWrap class.
    /// </summary>
    /// <param name="name">The name of the job.</param>
    /// <param name="execute">The Lua closure to execute.</param>
    /// <param name="state">Optional state to pass to the closure.</param>
    public LuaJobWrap(string name, Closure execute, object? state = null)
    {
        Name = name;
        _execute = execute;
        State = state;
    }

    /// <summary>
    /// Executes the wrapped Lua closure.
    /// </summary>
    public void Execute()
    {
        if (State == null)
        {
            _execute.Call();
        }
        else
        {
            _execute.Call(State);
        }
    }
}
