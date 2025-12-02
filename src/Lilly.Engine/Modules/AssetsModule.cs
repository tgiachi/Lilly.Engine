using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Interfaces.Services;

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

    [ScriptFunction("load_texture", "Loads a texture from the specified file path and returns its name.")]
    public string LoadTexture(string textureName, string texturePath)
    {
        _assetManager.LoadTextureFromFile(textureName, texturePath);

        return textureName;
    }


    [ScriptFunction("load_atlas", "Loads a texture atlas from the specified file path and returns its name.")]
    public string LoadTextureAtlas(
        string atlasName,
        string atlasPath,
        int tileWidth,
        int tileHeight,
        int margin = 0,
        int spacing = 0
    )
    {
        _assetManager.LoadTextureAtlasFromFile(atlasName, atlasPath, tileWidth, tileHeight, margin, spacing);

        return atlasName;
    }
}
