using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Engine.GameObjects.UI.Dialogs;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.GameObjects;

public class DefaultGameObjectPlugin : ILillyPlugin
{
    private IContainer _container;

    public LillyPluginData LillyData
        => new("squid.lilly.defaultgameobjects", "Lilly Default Game Objects", "1.0.0", "SquidDev", []);

    public void EngineInitialized(IContainer container) { }

    public IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        var text = gameObjectFactory.Create<TextEditGameObject>();
        text.Transform.Position = new(200, 200);

        yield return text;

        yield return gameObjectFactory.Create<NotificationHudGameObject>();

        var quakeConsole = gameObjectFactory.Create<QuakeConsoleGameObject>();

        quakeConsole.Name = "Global Quake Console";

        var inputManager = _container.Resolve<IInputManagerService>();

        inputManager.BindKey(
            "F3",
            () =>
            {
                quakeConsole.ToggleConsole();
            }
        );

        yield return quakeConsole;

        yield return gameObjectFactory.Create<ScriptErrorGameObject>();
    }

    public IContainer RegisterModule(IContainer container)
    {
        container.RegisterInstance(UITheme.Default);

        _container = container;

        return container
               .RegisterGameObject<ButtonGameObject>()
               .RegisterGameObject<CheckBoxGameObject>()
               .RegisterGameObject<ComboBoxGameObject>()
               .RegisterGameObject<ListBoxGameObject>()
               .RegisterGameObject<MemoEditGameObject>()
               .RegisterGameObject<ProgressBarGameObject>()
               .RegisterGameObject<StackPanelGameObject>()
               .RegisterGameObject<TextEditGameObject>()
               .RegisterGameObject<NotificationHudGameObject>()
               .RegisterGameObject<QuakeConsoleGameObject>()
               .RegisterGameObject<ScriptErrorGameObject>()
            ;
    }
}
