using Lilly.Engine.Core.Data.Commands;
using Lilly.Engine.Core.Extensions.Strings;
using Lilly.Engine.Core.Interfaces.Commands;
using Lilly.Engine.Core.Interfaces.Services;
using Serilog;

namespace Lilly.Engine.Services;

public class CommandSystemService : ICommandSystemService
{
    private readonly ILogger _logger = Log.ForContext<CommandSystemService>();

    private readonly List<CommandDefinitionRegistration> _registeredCommands;

    private readonly Dictionary<string, ICommand> _commands = new();

    public CommandSystemService(List<CommandDefinitionRegistration> registeredCommands = null)
        => _registeredCommands = registeredCommands;

    public IEnumerable<string> AutocompleteCommand(string input)
    {
        var inputLower = input.ToLowerInvariant();

        return _commands.Keys
                        .Where(cmd => cmd.StartsWith(inputLower, StringComparison.InvariantCultureIgnoreCase))
                        .OrderBy(cmd => cmd);
    }

    public async Task<CommandExecuteResult> ExecuteAsync(params string[] parameters)
    {
        var commandName = parameters.FirstOrDefault()?.ToSnakeCase();

        if (string.IsNullOrWhiteSpace(commandName))
        {
            return new(false, "No command provided.", null);
        }

        if (!_commands.TryGetValue(commandName, out var command))
        {
            return new(false, $"Command '{commandName}' not found.", null);
        }

        var commandParameters = parameters.Skip(1).ToArray();

        try
        {
            return await command.ExecuteAsync(commandParameters);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing command {CommandName}", commandName);

            return new(false, $"Error executing command '{commandName}': {ex.Message}", ex);
        }
    }

    public void RegisterCommand(ICommand command)
    {
        var commandName = command.CommandDefinition.Name.ToSnakeCase();
        _logger.Information(
            "Registering command {CommandName} - {Description}",
            commandName,
            command.CommandDefinition.Description
        );

        if (_commands.ContainsKey(commandName))
        {
            _logger.Warning("Command {CommandName} is already registered. Overwriting.", commandName);
        }

        _commands[commandName] = command;

        foreach (var alias in command.CommandDefinition.Aliases ?? [])
        {
            var aliasName = alias.ToSnakeCase();

            _logger.Information("Registering alias {AliasName} for command {CommandName}", aliasName, commandName);

            if (_commands.ContainsKey(aliasName))
            {
                _logger.Warning("Alias {AliasName} is already registered. Overwriting.", aliasName);
            }
            _commands[aliasName] = command;
        }
    }
}
