using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.Extensions;
using Lilly.Engine.GameObjects.Modules;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Engine.GameObjects.UI.Theme;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Lua.Scripting.Extensions.Scripts;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.GameObjects;

public class LillyGameObjectPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new("com.tgiachi.lilly.gameobjects", "Lilly Game Objects Plugin", "0.6.0", "squid squid@stormwind.it", []);

    public void EngineInitialized(IContainer container) { }
    public void EngineReady(IContainer container) { }

    public IEnumerable<IGameObject> GetGlobalGameObjects(IGameObjectFactory gameObjectFactory)
    {
        yield return gameObjectFactory.Create<NotificationHudGameObject>();
        yield return gameObjectFactory.Create<QuakeConsoleGameObject>();
    }

    public IContainer RegisterModule(IContainer container)
    {
        container.Register<UITheme>(Reuse.Singleton);
        container.RegisterGameObject<NotificationHudGameObject>();
        container.RegisterGameObject<QuakeConsoleGameObject>();
        container.RegisterGameObject<ButtonGameObject>();
        container.RegisterGameObject<TextEditGameObject>();

        container.RegisterScriptModule<QuakeConsoleModule>();


        return container;
    }
}
