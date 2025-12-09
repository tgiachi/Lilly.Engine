using Lilly.Engine.Core.Data.Commands;
using Lilly.Engine.Core.Interfaces.Commands;

namespace Lilly.Engine.Core.Interfaces.Services;

/// <summary>
/// Provides functionality for executing and managing commands.
/// </summary>
public interface ICommandSystemService
{
    /// <summary>
    /// Provides autocomplete suggestions for a partial command input.
    /// </summary>
    /// <param name="input">The partial command input.</param>
    /// <returns>A collection of autocomplete suggestions.</returns>
    IEnumerable<string> AutocompleteCommand(string input);

    /// <summary>
    /// Executes a command asynchronously with the given parameters.
    /// </summary>
    /// <param name="parameters">The command and its parameters.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandExecuteResult> ExecuteAsync(params string[] parameters);

    /// <summary>
    /// Registers a command with the system.
    /// </summary>
    /// <param name="command">The command to register.</param>
    void RegisterCommand(ICommand command);
}
