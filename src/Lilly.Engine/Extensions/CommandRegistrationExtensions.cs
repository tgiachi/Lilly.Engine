using DryIoc;
using Lilly.Engine.Core.Data.Commands;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Core.Interfaces.Commands;

namespace Lilly.Engine.Extensions;

public static class CommandRegistrationExtensions
{
    extension(IContainer container)
    {

        public IContainer RegisterCommand<TCommand>()
            where TCommand : ICommand
        {
            container.AddToRegisterTypedList(new CommandDefinitionRegistration(typeof(TCommand)));

             container.Register<TCommand>(Reuse.Singleton);

            return container;

        }
    }

}
