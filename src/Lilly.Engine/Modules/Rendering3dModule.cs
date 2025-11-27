using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Pipelines;

namespace Lilly.Engine.Modules;

[ScriptModule("rendering", "Provides functions to interact with 2D/3D rendering features.")]
public class Rendering3dModule
{

    private readonly ThreeDLayer _3dLayer;
    public Rendering3dModule(ThreeDLayer layer)
    {
        _3dLayer = layer;
    }

    [ScriptFunction("toggle_wireframe", "Toggles the wireframe rendering mode for 3D objects.")]
    public void ToggleWireframe()
    {
        _3dLayer.IsWireframe = !_3dLayer.IsWireframe;
    }

    [ScriptFunction("is_wireframe", "Checks if the wireframe rendering mode is enabled for 3D objects.")]
    public bool IsWireframe()
    {
        return _3dLayer.IsWireframe;
    }




}
