using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Core.Interfaces.Services;
using Lilly.Engine.Wrappers.Commands;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Modules;

[ScriptModule("commands", "Provides access to the command system.")]
public class CommandsModule
{
    private readonly ICommandSystemService _commandSystemService;

    public CommandsModule(ICommandSystemService commandSystemService)
        => _commandSystemService = commandSystemService;

    [ScriptFunction("register", "Registers a new command in the command system.")]
    public void RegisterCommand(string commandName, string description, string aliases, Closure executeFunction)
    {
        var commandWrap = new CommandLuaWrap(commandName, description, aliases, executeFunction);
        _commandSystemService.RegisterCommand(commandWrap);
    }
}
