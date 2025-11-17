using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.GameObjects.Base;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Engine.GameObjects.UI.Dialogs;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Rendering.Core.Extensions;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects;

public class DefaultGameObjectPlugin : ILillyPlugin
{
    private IContainer _container;

    public LillyPluginData LillyData
        => new("squid.lilly.defaultgameobjects", "Lilly Default Game Objects", "1.0.0", "SquidDev", []);

    public void EngineInitialized(IContainer container) { }

    public IEnumerable<IGameObject> GlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield return gameObjectFactory.Create<NotificationHudGameObject>();

        yield return gameObjectFactory.Create<ScriptErrorGameObject>();

        var textConsole = gameObjectFactory.Create<TextGameObject>();
        textConsole.Transform.Position = new(100, 100);
        textConsole.Color = Color4b.Blue;
        ;
        textConsole.Text = "Quake Console (Press ` to toggle)";

        yield return textConsole;

        var textBox = gameObjectFactory.Create<TextEditGameObject>();
        textBox.Transform.Position = new(100, 200);

        yield return textBox;

    }

    public IContainer RegisterModule(IContainer container)
    {
        container.RegisterInstance(UITheme.Default);

        _container = container;

        return container
               .RegisterGameObject<ButtonGameObject>()
               .RegisterGameObject<TextGameObject>()
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
