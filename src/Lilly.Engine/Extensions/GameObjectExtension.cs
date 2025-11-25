using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Data.Internal;
using Lilly.Rendering.Core.Interfaces.Entities;

namespace Lilly.Engine.Extensions;

public static class GameObjectExtension
{
    extension(IContainer container)
    {
        public IContainer RegisterGameObject<TGameObject>()
            where TGameObject : IGameObject
        {
            container.Register<TGameObject>(
                Reuse.Transient,
                setup: Setup.With(
                    allowDisposableTransient: true,
                    trackDisposableTransient: true
                )
            );

            container.AddToRegisterTypedList(new GameObjectRegistration(typeof(TGameObject)));

            return container;
        }

        public IContainer RegisterGameObject(Type gameObjectType)
        {
            container.Register(
                gameObjectType,
                Reuse.Transient,
                setup: Setup.With(
                    allowDisposableTransient: true,
                    trackDisposableTransient: true
                )
            );

            container.AddToRegisterTypedList(new GameObjectRegistration(gameObjectType));

            return container;
        }
    }

}
