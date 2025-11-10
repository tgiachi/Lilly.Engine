using Lilly.Engine.Core.Interfaces.Jobs;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Wrappers;

/// <summary>
/// Wraps a Lua closure to be executed as a job in the job system.
/// </summary>
public class LuaJobWrap : IJob
{
    private readonly Closure _execute;

    public object? State { get; set; }

    public string Name { get; }

    public LuaJobWrap(string name, Closure execute, object? state = null)
    {
        Name = name;
        _execute = execute;
        State = state;
    }

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
