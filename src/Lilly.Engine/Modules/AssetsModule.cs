using Lilly.Engine.Core.Attributes.Scripts;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Exceptions;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Json.Assets;
using Serilog;

namespace Lilly.Engine.Modules;

[ScriptModule("assets", "Provides functions to interact with game assets.")]
public class AssetsModule
{
    private readonly IAssetManager _assetManager;

    private readonly DirectoriesConfig _directoriesConfig;

    private readonly ILogger _logger = Log.ForContext<AssetsModule>();

    public AssetsModule(IAssetManager assetManager, DirectoriesConfig directoriesConfig)
    {
        _assetManager = assetManager;
        _directoriesConfig = directoriesConfig;
    }

    [ScriptFunction("load_font", "Loads a font from the specified file path and returns its name.")]
    public string LoadFont(string fontName, string fontPath)
    {
        _assetManager.LoadFontFromFile(fontName, fontPath);

        return fontName;
    }

    [ScriptFunction("load_model", "Loads a 3D model from the specified file path and returns its name.")]
    public void LoadModel(string modelName, string modelPath)
    {
        _assetManager.LoadModelFromFile(modelName, modelPath);
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

    public void LoadAssetsFromJson(string jsonPath)
    {
        var fullPath = Path.Combine(_directoriesConfig.Root, jsonPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Asset JSON file not found at path: {fullPath}");
        }

        var assetFile = JsonUtils.DeserializeFromFile<LillyEngineAssetJson>(jsonPath);

        _logger.Debug("Loading assets from file: {FullPath} with {Count} entries", fullPath, assetFile.Assets.Length);

        foreach (var asset in assetFile.Assets)
        {
            switch (asset.Type)
            {
                case AssetType.Font:
                    _assetManager.LoadFontFromFile(asset.Name, asset.Path);

                    break;
                case AssetType.Texture:
                    _assetManager.LoadTextureFromFile(asset.Name, asset.Path);

                    break;
                case AssetType.Model:
                    if (asset.Path.EndsWith(".zip"))
                    {
                        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                        _assetManager.LoadModelFromZip(asset.Name, asset.Path, tmpDir);

                        break;
                    }
                    _assetManager.LoadModelFromFile(asset.Name, asset.Path);

                    break;

                case AssetType.Atlas:
                    {
                        var tileWidth = Convert.ToInt32(asset.Metadata.GetValueOrDefault("tileWidth") ?? 0);
                        var tileHeight = Convert.ToInt32(asset.Metadata.GetValueOrDefault("tileHeight") ?? 0);
                        var spacing = Convert.ToInt32(asset.Metadata.GetValueOrDefault("spacing") ?? 0);
                        var margin = Convert.ToInt32(asset.Metadata.GetValueOrDefault("margin") ?? 0);

                        if (tileHeight == 0 || tileWidth == 0)
                        {
                            throw new AssetAtlasInvalidSizeException("Invalid tile height/width/height");
                        }

                        _assetManager.LoadTextureAtlasFromFile(
                            asset.Name,
                            asset.Path,
                            tileWidth,
                            tileHeight,
                            spacing,
                            margin
                        );
                    }

                    break;
                case AssetType.Sound:
                    _assetManager.LoadSoundFromFile(asset.Name, asset.Path);

                    break;
                case AssetType.Shader:
                default:
                    _logger.Warning("Unsupported asset type: {Type} for asset: {Name}", asset.Type, asset.Name);

                    break;
            }
        }
    }
}
