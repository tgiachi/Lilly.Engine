using System.IO.Compression;
using System.Text.Json;
using Lilly.Engine.Audio;
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

    [ScriptFunction("load_sound", "Loads a sound from the specified file path.")]
    public void LoadSound(string soundName, string soundPath)
    {
        AudioType audioType = AudioType.Ogg;

        if (soundPath.EndsWith(".mp3"))
        {
            audioType = AudioType.Mp3;
        }

        _assetManager.LoadSoundFromFile(soundName, soundPath, audioType);
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
                case AssetType.Material:
                    _assetManager.LoadMaterialFromFile(asset.Name, asset.Path);

                    break;
                case AssetType.Shader:
                default:
                    _logger.Warning("Unsupported asset type: {Type} for asset: {Name}", asset.Type, asset.Name);

                    break;
            }
        }
    }

    public void LoadAssetsFromZip(string zipPath, string manifestName = "assets.json")
    {
        var fullZipPath = Path.Combine(_directoriesConfig.Root, zipPath);

        if (!File.Exists(fullZipPath))
        {
            throw new FileNotFoundException($"Asset zip file not found at path: {fullZipPath}");
        }

        using var archive = ZipFile.OpenRead(fullZipPath);
        var manifestEntry = archive.GetEntry(manifestName);

        if (manifestEntry == null)
        {
            throw new FileNotFoundException($"Manifest '{manifestName}' not found inside zip '{fullZipPath}'");
        }

        LillyAssetManifestJson? manifest;

        using (var manifestStream = manifestEntry.Open())
        {
            using (var reader = new StreamReader(manifestStream))
            {
                var manifestJson = reader.ReadToEnd();
                manifest = JsonUtils.Deserialize<LillyAssetManifestJson>(manifestJson);
            }
        }

        if (manifest == null)
        {
            throw new JsonException($"Failed to deserialize manifest '{manifestName}' in zip '{fullZipPath}'");
        }

        _logger.Debug("Loading assets from zip: {FullPath} with {Count} entries", fullZipPath, manifest.Assets.Length);

        foreach (var asset in manifest.Assets)
        {
            var entry = archive.GetEntry(asset.Path);

            if (asset.Type != AssetType.Model && entry == null)
            {
                _logger.Warning("Asset entry '{Path}' not found in zip {Zip}", asset.Path, fullZipPath);

                continue;
            }

            switch (asset.Type)
            {
                case AssetType.Font:
                    using (var stream = entry!.Open())
                    {
                        _assetManager.LoadFontFromMemory(asset.Name, stream);
                    }

                    break;
                case AssetType.Texture:
                    using (var stream = entry!.Open())
                    {
                        _assetManager.LoadTextureFromMemory(asset.Name, stream);
                    }

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

                        using var stream = entry!.Open();
                        _assetManager.LoadTextureAtlasFromMemory(asset.Name, stream, tileWidth, tileHeight, spacing, margin);
                    }

                    break;
                case AssetType.Sound:
                    using (var stream = entry!.Open())
                    {
                        var audioType = asset.Path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                                            ? AudioType.Mp3
                                            : AudioType.Ogg;
                        _assetManager.LoadSoundFromStream(asset.Name, stream, audioType);
                    }

                    break;
                case AssetType.Material:
                    using (var stream = entry!.Open())
                    {
                        _assetManager.LoadMaterialFromStream(asset.Name, stream);
                    }

                    break;
                case AssetType.Model:
                    if (archive.GetEntry(asset.Path) == null)
                    {
                        _logger.Warning("Model file {ModelPath} not found inside zip {ZipPath}", asset.Path, fullZipPath);

                        break;
                    }

                    // Let the asset manager extract and load the model from the zip itself
                    _assetManager.LoadModelFromZip(asset.Name, zipPath, asset.Path);

                    break;
                case AssetType.Shader:
                default:
                    _logger.Warning("Unsupported asset type: {Type} for asset: {Name}", asset.Type, asset.Name);

                    break;
            }
        }
    }
}
