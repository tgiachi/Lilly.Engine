using Lilly.Engine.Core.Data.Commands;
using Lilly.Engine.Core.Interfaces.Commands;
using MoonSharp.Interpreter;

namespace Lilly.Engine.Wrappers.Commands;

public class CommandLuaWrap : ICommand
{
    private readonly Closure _executeFunction;

    public CommandLuaWrap(string commandName, string description, string aliases, Closure executeFunction)
    {
        var aliasesList = aliases.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        CommandDefinition = new(commandName, description, aliasesList);
        _executeFunction = executeFunction;
    }

    public CommandDefinition CommandDefinition { get; }

    public Task<CommandExecuteResult> ExecuteAsync(params string[] parameters)
    {
        try
        {
            var result = _executeFunction.Call((object)parameters);

            if (result.Type == DataType.Table)
            {
                var table = result.Table;
                var success = table.Get("Success").Boolean;
                var message = table.Get("Message").String;

                return Task.FromResult(new CommandExecuteResult(success, message ?? string.Empty, null));
            }

            return Task.FromResult(new CommandExecuteResult(false, "Invalid return type from Lua function.", null));
        }
        catch (ScriptRuntimeException ex)
        {
            return Task.FromResult(new CommandExecuteResult(false, $"Lua error: {ex.Message}", ex));
        }
    }
}
