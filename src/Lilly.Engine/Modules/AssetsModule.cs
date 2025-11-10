using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Interfaces.Services;

namespace Lilly.Engine.Modules;


[ScriptModule("assets", "Provides functions to interact with game assets.")]
public class AssetsModule
{
    private readonly IAssetManager _assetManager;
    public AssetsModule(IAssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    [ScriptFunction("load_font", "Loads a font from the specified file path and returns its name.")]
    public string LoadFont(string fontName, string fontPath)
    {
        _assetManager.LoadFontFromFile(fontName, fontPath);

        return fontName;
    }
}
