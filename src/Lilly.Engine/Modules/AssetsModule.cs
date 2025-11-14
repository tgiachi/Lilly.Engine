using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Rendering.Core.Interfaces.Services;

namespace Lilly.Engine.Modules;

[ScriptModule("assets", "Provides functions to interact with game assets.")]
public class AssetsModule
{
    private readonly IAssetManager _assetManager;

    public AssetsModule(IAssetManager assetManager)
        => _assetManager = assetManager;

    [ScriptFunction("load_font", "Loads a font from the specified file path and returns its name.")]
    public string LoadFont(string fontName, string fontPath)
    {
        _assetManager.LoadFontFromFile(fontName, fontPath);

        return fontName;
    }

    [ScriptFunction("load_lilly_shader", "Loads a Lilly shader from the specified file path and returns its name.")]
    public string LoadLillyShader(string shaderName, string shaderPath)
    {
        _assetManager.LoadLillyShaderFromFile(shaderName, shaderPath);

        return shaderName;
    }
}
