using DryIoc;
using Lilly.Engine.Data.Plugins;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Engine.Interfaces.Plugins;
using Lilly.Engine.Rendering.Core.Extensions;

namespace Lilly.Engine.GameObjects;

public class DefaultGameObjectPlugin : ILillyPlugin
{
    public LillyPluginData LillyData
        => new LillyPluginData("squid.lilly.defaultgameobjects", "Lilly Default Game Objects", "1.0.0", "SquidDev", []);

    public IContainer RegisterModule(IContainer container)
    {
        return container
               .RegisterGameObject<ButtonGameObject>()
               .RegisterGameObject<CheckBoxGameObject>()
               .RegisterGameObject<ComboBoxGameObject>()
               .RegisterGameObject<ListBoxGameObject>()
               .RegisterGameObject<MemoEditGameObject>()
               .RegisterGameObject<ProgressBarGameObject>()
               .RegisterGameObject<TextEditGameObject>();
    }

    public void EngineInitialized(IContainer container)
    {
    }
}
