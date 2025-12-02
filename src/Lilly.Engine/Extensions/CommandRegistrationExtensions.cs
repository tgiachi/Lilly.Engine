using DryIoc;
using Lilly.Engine.Core.Data.Commands;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Commands;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Provides extension methods for registering commands in the DryIoc container.
/// </summary>
public static class CommandRegistrationExtensions
{
    /// <summary>
    /// Registers a command with the container as a singleton.
    /// </summary>
    /// <param name="container">The dependency injection container.</param>
    /// <typeparam name="TCommand">The type of the command to register.</typeparam>
    /// <returns>The container for method chaining.</returns>
    public static IContainer RegisterCommand<TCommand>(this IContainer container)
        where TCommand : ICommand
    {
        container.AddToRegisterTypedList(new CommandDefinitionRegistration(typeof(TCommand)));

        container.Register<TCommand>(Reuse.Singleton);

        return container;
    }
}
