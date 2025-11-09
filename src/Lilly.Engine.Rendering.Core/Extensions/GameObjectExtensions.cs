using DryIoc;
using Lilly.Engine.Core.Extensions.Container;
using Lilly.Engine.Rendering.Core.Data.Internal;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Extensions;

public static class GameObjectExtensions
{
    public static IContainer RegisterGameObject<TGameObject>(this IContainer container, bool usePooling = true)
        where TGameObject : class, IGameObject
    {
        container.Register<IGameObject, TGameObject>(Reuse.Transient);
        container.AddToRegisterTypedList(new GameObjectRegistration(typeof(TGameObject), usePooling));

        return container;
    }
}
