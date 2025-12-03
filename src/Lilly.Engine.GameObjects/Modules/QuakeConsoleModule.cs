using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.GameObjects.UI.Controls;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.GameObjects.Modules;

[ScriptModule("quake_console", "Provides functionality for the Quake-style console.")]
public class QuakeConsoleModule
{
    private readonly IRenderPipeline _renderPipeline;

    public QuakeConsoleModule(IRenderPipeline renderPipeline)
    {
        _renderPipeline = renderPipeline;
    }

    [ScriptFunction("toggle", "Toggles the visibility of the console.")]
    public void ToggleConsole()
    {
        var quakeConsole = _renderPipeline.GetGameObjectOfType<QuakeConsoleGameObject>();

        quakeConsole?.ToggleConsole();

    }
}
