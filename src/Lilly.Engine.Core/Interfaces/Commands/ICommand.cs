using Lilly.Engine.Core.Data.Commands;

namespace Lilly.Engine.Core.Interfaces.Commands;

public interface ICommand
{
    Task<CommandExecuteResult> ExecuteAsync(params string[] parameters);

    CommandDefinition CommandDefinition { get; }

}
