using Lilly.Engine.Core.Data.Commands;
using Lilly.Engine.Core.Interfaces.Commands;

namespace Lilly.Engine.Core.Interfaces.Services;

public interface ICommandSystemService
{
    Task<CommandExecuteResult> ExecuteAsync(params string[] parameters);

    IEnumerable<string> AutocompleteCommand(string input);

    void RegisterCommand(ICommand command);
}
