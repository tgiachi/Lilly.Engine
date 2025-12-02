using Lilly.Engine.Core.Data.Commands;

namespace Lilly.Engine.Core.Interfaces.Commands;

/// <summary>
/// Defines a command that can be executed with parameters.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Executes the command asynchronously with the given parameters.
    /// </summary>
    /// <param name="parameters">The parameters to pass to the command.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CommandExecuteResult> ExecuteAsync(params string[] parameters);

    /// <summary>
    /// Gets the definition of the command.
    /// </summary>
    CommandDefinition CommandDefinition { get; }

}
