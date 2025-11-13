using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Silk.NET.Maths;

namespace Lilly.Engine.GameObjects;

public class DefaultGameObjectPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new LillyPluginData("squid.lilly.defaultgameobjects", "Lilly Default Game Objects", "1.0.0", "SquidDev", []);

    public IContainer RegisterModule(IContainer container)
    {
        container.RegisterInstance(UITheme.Default);

        return container
               .RegisterGameObject<ButtonGameObject>()
               .RegisterGameObject<CheckBoxGameObject>()
               .RegisterGameObject<ComboBoxGameObject>()
               .RegisterGameObject<ListBoxGameObject>()
               .RegisterGameObject<MemoEditGameObject>()
               .RegisterGameObject<ProgressBarGameObject>()
               .RegisterGameObject<TextEditGameObject>()
               .RegisterGameObject<NotificationHudGameObject>()
            ;
    }

    public void EngineInitialized(IContainer container) { }

    public IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield return gameObjectFactory.CreateGameObject<NotificationHudGameObject>();

    }
}
