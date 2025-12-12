using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Assimp;
using Assimp.Configs;
using AssimpNet;
using FontStashSharp;
using Lilly.Engine.Attributes;
using Lilly.Engine.Audio;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Core.Json;
using Lilly.Engine.Data.Assets;
using Lilly.Engine.Data.Atlas;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Json.Materials;
using Lilly.Engine.Utils;
using Lilly.Engine.Vertexts;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Materials;
using Serilog;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TrippyGL;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using AssimpMatrix4x4 = Assimp.Matrix4x4;
using AssimpVector3D = Assimp.Vector3D;
using AssimpTextureType = Assimp.TextureType;
using BoundingBox = Lilly.Rendering.Core.Primitives.BoundingBox;
using Material = Lilly.Rendering.Core.Materials.Material;

namespace Lilly.Engine.Services;

/// <summary>
/// Manages loading and caching of assets such as textures, fonts, and shaders.
/// </summary>
public class AssetManager : IAssetManager, IDisposable
{
    private readonly int[] defaultSizes = [8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 40, 48, 64, 72, 96, 128, 144, 192, 256];

    private readonly ILogger _logger = Log.ForContext<AssetManager>();
    private readonly DirectoriesConfig _directoriesConfig;
    private readonly RenderContext _context;
    private readonly IAudioService _audioService;

    private readonly Dictionary<string, FontSystem> _fontSystems = new();
    private readonly Dictionary<string, DynamicSpriteFont> _dynamicSpriteFonts = new();

    private readonly Dictionary<string, Texture2D> _texture2Ds = new();
    private readonly Dictionary<string, ShaderProgram> _shaderPrograms = new();

    private readonly Dictionary<string, AtlasDefinition> _textureAtlases = new();

    private readonly AssimpContext _assimpContext = new();
    private readonly Dictionary<string, string> _modelExtractionDirectories = new();
    private readonly List<string> _tempAudioFiles = new();
    private readonly Dictionary<string, Material> _materials = new();

    private readonly PostProcessSteps _defaultPostProcessSteps = PostProcessSteps.Triangulate |
                                                                 PostProcessSteps.CalculateTangentSpace |
                                                                 PostProcessSteps.JoinIdenticalVertices |
                                                                 PostProcessSteps.FlipUVs;

    private readonly Dictionary<string, ModelAsset> _loadedModels = new();

    // Cached white texture (1x1 white pixel) for drawing colored rectangles and fallback
    private Texture2D? _whiteTexture;

    private Texture2D? _blackTexture;

    private readonly FontSystemSettings _defaultFontSettings = new()
    {
        FontResolutionFactor = 2,
        KernelWidth = 2,
        KernelHeight = 2
    };

    /// <summary>
    /// Initializes a new instance of the AssetManager class.
    /// </summary>
    /// <param name="directoriesConfig">The directories configuration.</param>
    /// <param name="context">The render context.</param>
    public AssetManager(DirectoriesConfig directoriesConfig, RenderContext context, IAudioService audioService)
    {
        _directoriesConfig = directoriesConfig;
        _context = context;
        _audioService = audioService;
        GetWhiteTexture<Texture2D>();
        _texture2Ds[DefaultTextures.WhiteTextureKey] = _whiteTexture!;
        _assimpContext.SetConfig(new NormalSmoothingAngleConfig(66.0f));

        Platform.Init();
    }

    /// <summary>
    /// Creates a vertex buffer from vertex data.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="vertices">The vertex data array.</param>
    /// <param name="usage">The buffer usage hint.</param>
    /// <returns>The created vertex buffer.</returns>
    public VertexBuffer<TVertex> CreateVertexBuffer<TVertex>(TVertex[] vertices, BufferUsage usage = BufferUsage.StaticCopy)
        where TVertex : unmanaged, IVertex
    {
        var vertexBuffer = new VertexBuffer<TVertex>(_context.GraphicsDevice, vertices, usage);
        _logger.Debug("Created vertex buffer with {VertexCount} vertices", vertices.Length);

        return vertexBuffer;
    }

    /// <summary>
    /// Disposes of the asset manager and releases all loaded assets.
    /// </summary>
    public void Dispose()
    {
        foreach (var (_, dir) in _modelExtractionDirectories)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to cleanup extraction directory {Dir}", dir);
            }
        }
        _modelExtractionDirectories.Clear();

        foreach (var temp in _tempAudioFiles)
        {
            try
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to delete temp audio file {Temp}", temp);
            }
        }
        _tempAudioFiles.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets a specific region from a loaded texture atlas.
    /// </summary>
    /// <param name="atlasName">The name of the atlas.</param>
    /// <param name="tileIndex">The index of the tile in the atlas.</param>
    /// <returns>The atlas region with UV coordinates and size.</returns>
    public AtlasRegion GetAtlasRegion(string atlasName, int tileIndex)
    {
        if (!_textureAtlases.TryGetValue(atlasName, out var atlasDefinition))
        {
            throw new InvalidOperationException($"Texture atlas '{atlasName}' is not loaded.");
        }

        if (tileIndex < 0 || tileIndex >= atlasDefinition.Regions.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileIndex),
                $"Tile index {tileIndex} is out of range for atlas '{atlasName}' with {atlasDefinition.Regions.Count} tiles."
            );
        }

        return atlasDefinition.Regions[tileIndex];
    }

    /// <summary>
    /// Gets a specific region from a loaded texture atlas by coordinates.
    /// </summary>
    /// <param name="atlasName">The name of the atlas.</param>
    /// <param name="xIndex">The x index of the tile.</param>
    /// <param name="yIndex">The y index of the tile.</param>
    /// <returns>The atlas region with UV coordinates and size.</returns>
    public AtlasRegion GetAtlasRegion(string atlasName, int xIndex, int yIndex)
    {
        if (!_textureAtlases.TryGetValue(atlasName, out var atlasDefinition))
        {
            throw new InvalidOperationException($"Texture atlas '{atlasName}' is not loaded.");
        }

        var columns = (int)((_texture2Ds[atlasDefinition.TextureName].Width -
                             2 * atlasDefinition.Margin +
                             atlasDefinition.Spacing) /
                            (float)(atlasDefinition.Width + atlasDefinition.Spacing));

        var tileIndex = yIndex * columns + xIndex;

        if (tileIndex < 0 || tileIndex >= atlasDefinition.Regions.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tileIndex),
                $"Tile index {tileIndex} is out of range for atlas '{atlasName}' with {atlasDefinition.Regions.Count} tiles."
            );
        }

        return atlasDefinition.Regions[tileIndex];
    }

    /// <summary>
    /// Retrieves a font with the specified name and size.
    /// </summary>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="size">The size of the font.</param>
    /// <typeparam name="TFont">The type of the font.</typeparam>
    /// <returns>The font instance.</returns>
    public DynamicSpriteFont GetFont(string fontName, int size)
    {
        var key = $"{fontName}_{size}";

        if (_dynamicSpriteFonts.TryGetValue(key, out var font))
        {
            return font;
        }

        if (_fontSystems.TryGetValue(fontName, out var fontSystem))
        {
            var dynamicFont = fontSystem.GetFont(size);
            _dynamicSpriteFonts[key] = dynamicFont;

            return dynamicFont;
        }

        throw new InvalidOperationException($"Font '{fontName}' with size {size} is not loaded.");
    }

    public ModelAsset GetModel(string modelName)
        => _loadedModels.TryGetValue(modelName, out var model)
               ? model
               : throw new InvalidOperationException($"Model '{modelName}' is not loaded.");

    public Material GetMaterial(string materialName)
        => _materials.TryGetValue(materialName, out var material)
               ? material
               : throw new InvalidOperationException($"Material '{materialName}' is not loaded.");

    public void LoadSoundFromFile(string soundName, string soundPath, AudioType audioType = AudioType.Ogg)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], soundPath);

        if (!File.Exists(fullPath))
        {
            _logger.Warning("Sound file not found at {Path}", fullPath);

            return;
        }

        try
        {
            _audioService.LoadSoundEffect(soundName, fullPath, audioType);

            _logger.Information("Loaded sound {SoundName} from {Path}", soundName, fullPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading sound {SoundName} from {Path}", soundName, fullPath);
        }
    }

    public void LoadSoundFromStream(string soundName, Stream stream, AudioType audioType = AudioType.Ogg)
    {
        try
        {
            var tempPath = CreateTempAudioFile(stream, audioType);
            LoadSoundFromFile(soundName, tempPath, audioType);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading sound {SoundName} from stream", soundName);
        }
    }

    private string CreateTempAudioFile(Stream stream, AudioType audioType)
    {
        var extension = audioType switch
        {
            AudioType.Mp3 => ".mp3",
            _             => ".ogg"
        };

        var tempPath = Path.ChangeExtension(Path.GetTempFileName(), extension);

        using (var fs = File.Open(tempPath, FileMode.Create, FileAccess.Write))
        {
            stream.CopyTo(fs);
        }

        _tempAudioFiles.Add(tempPath);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return tempPath;
    }

    /// <summary>
    /// Retrieves a previously loaded shader program by name.
    /// </summary>
    /// <param name="shaderName">The name of the shader program.</param>
    /// <returns>The shader program.</returns>
    public ShaderProgram GetShaderProgram(string shaderName)
        => _shaderPrograms.TryGetValue(shaderName, out var shaderProgram)
               ? shaderProgram
               : throw new InvalidOperationException($"Shader '{shaderName}' is not loaded.");

    /// <summary>
    /// Retrieves a previously loaded texture by name.
    /// </summary>
    /// <param name="textureName">The name of the texture.</param>
    /// <typeparam name="TTexture">The type of the texture.</typeparam>
    /// <returns>The texture instance.</returns>
    public TTexture GetTexture<TTexture>(string textureName) where TTexture : class
    {
        if (_texture2Ds.TryGetValue(textureName, out var texture))
        {
            return texture as TTexture;
        }

        throw new InvalidOperationException($"Texture '{textureName}' is not loaded.");
    }

    /// <summary>
    /// Gets the OpenGL handle of a loaded texture.
    /// </summary>
    /// <param name="textureName">The name of the texture.</param>
    /// <returns>The texture handle.</returns>
    public uint GetTextureHandle(string textureName)
        => _texture2Ds.TryGetValue(textureName, out var texture)
               ? texture.Handle
               : throw new InvalidOperationException($"Texture '{textureName}' is not loaded.");

    /// <summary>
    /// Gets a 1x1 white texture that can be used for drawing colored shapes or as a fallback.
    /// The texture is created once and cached for subsequent calls.
    /// </summary>
    /// <typeparam name="TTexture">The texture type (typically Texture2D).</typeparam>
    /// <returns>A white 1x1 texture.</returns>
    public TTexture GetWhiteTexture<TTexture>() where TTexture : class
    {
        // Lazy initialization: create texture only when first requested
        if (_whiteTexture == null)
        {
            // Create a 1x1 white texture
            _whiteTexture = new(_context.GraphicsDevice, 1, 1);

            // Set the single pixel to white (RGBA: 255, 255, 255, 255)
            var whitePixel = new Color4b(255, 255, 255);
            ReadOnlySpan<Color4b> pixelData = [whitePixel];
            _whiteTexture.SetData(pixelData, PixelFormat.Rgba);

            _logger.Debug("Created white texture (1x1)");
        }

        return _whiteTexture as TTexture ??
               throw new InvalidOperationException($"Cannot cast white texture to {typeof(TTexture).Name}");
    }

    public Texture2D GetWhiteTexture()
        => GetWhiteTexture<Texture2D>();

    public Texture2D GetBlackTexture()
    {
        if (_blackTexture == null)
        {
            _blackTexture = CreateTexture("black_texture", new Color4b(0, 0, 0));
        }

        return _blackTexture;
    }

    public Texture2D CreateTexture(string name, Color4b color)
    {
        var texture = new Texture2D(_context.GraphicsDevice, 1, 1);

        ReadOnlySpan<Color4b> pixelData = [color];
        texture.SetData(pixelData, PixelFormat.Rgba);

        _texture2Ds[name] = texture;

        _logger.Debug(
            "Created texture {TextureName} (1x1) with color R:{R} G:{G} B:{B} A:{A}",
            name,
            color.R,
            color.G,
            color.B,
            color.A
        );

        return texture;
    }

    public void LoadMaterialFromFile(string materialName, string materialPath)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], materialPath);

        if (!File.Exists(fullPath))
        {
            _logger.Warning("Material file not found at {Path}", fullPath);

            return;
        }

        try
        {
            var json = File.ReadAllText(fullPath);
            LoadMaterialFromJson(materialName, json);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading material {MaterialName} from file {Path}", materialName, fullPath);
        }
    }

    public void LoadMaterialFromStream(string materialName, Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            var json = reader.ReadToEnd();
            LoadMaterialFromJson(materialName, json);

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading material {MaterialName} from stream", materialName);
        }
    }

    /// <summary>
    /// Loads a font from a file and registers it with the asset manager.
    /// </summary>
    /// <param name="fontName">The name to associate with the loaded font.</param>
    /// <param name="fontPath">The path to the font file.</param>
    public void LoadFontFromFile(string fontName, string fontPath)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], fontPath);

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        LoadFontFromMemory(fontName, stream);
    }

    /// <summary>
    /// Loads a font from a stream and registers it with the asset manager.
    /// </summary>
    /// <param name="fontName">The name to associate with the loaded font.</param>
    /// <param name="stream">The stream containing the font data.</param>
    public void LoadFontFromMemory(string fontName, Stream stream)
    {
        var fontSystem = new FontSystem(_defaultFontSettings);

        _fontSystems[fontName] = fontSystem;

        fontSystem.AddFont(stream);

        _logger.Information("Loaded font {FontName}", fontName);

        foreach (var size in defaultSizes)
        {
            var font = fontSystem.GetFont(size);

            _logger.Verbose("  - Added size {FontSize}", size);
            _dynamicSpriteFonts[$"{fontName}_{size}"] = font;
        }
    }

    public void LoadModelFromFile(string modelName, string modelPath)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], modelPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Model file not found at path: {fullPath}");
        }

        var scene = _assimpContext.ImportFile(fullPath, _defaultPostProcessSteps);

        if (scene == null || scene.RootNode == null || scene.MeshCount == 0)
        {
            _logger.Warning("Failed to load model {ModelName} from {Path}", modelName, fullPath);

            return;
        }

        var modelDirectory = Path.GetDirectoryName(fullPath) ?? _directoriesConfig[DirectoryType.Assets];

        var modelAsset = BuildModelAsset(modelName, scene, modelDirectory);

        if (_loadedModels.TryGetValue(modelName, out var existingModel))
        {
            existingModel.Dispose();
        }
        _loadedModels[modelName] = modelAsset;

        _logger.Information(
            "Loaded model {ModelName} with {MeshCount} meshes and {InstanceCount} instances",
            modelName,
            modelAsset.Meshes.Count,
            modelAsset.Instances.Count
        );

        CleanupExtractionDirectory(modelName);
    }

    public void LoadModelFromZip(string modelName, string zipPath, string modelPathInZip)
    {
        var fullZipPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], zipPath);

        if (!File.Exists(fullZipPath))
        {
            _logger.Warning("Zip file not found for model {ModelName} at {Path}", modelName, fullZipPath);

            return;
        }

        var extractionDir = CreateExtractionDirectory(modelName);

        try
        {
            ZipFile.ExtractToDirectory(fullZipPath, extractionDir, overwriteFiles: true);

            var modelFullPath = Path.Combine(extractionDir, modelPathInZip);

            if (!File.Exists(modelFullPath))
            {
                _logger.Warning("Model file {ModelPathInZip} not found inside zip {ZipPath}", modelPathInZip, zipPath);

                return;
            }

            var scene = _assimpContext.ImportFile(modelFullPath, _defaultPostProcessSteps);

            if (scene == null || scene.RootNode == null || scene.MeshCount == 0)
            {
                _logger.Warning("Failed to load model {ModelName} from zip {ZipPath}", modelName, zipPath);

                return;
            }

            var modelDirectory = Path.GetDirectoryName(modelFullPath) ?? extractionDir;
            var modelAsset = BuildModelAsset(modelName, scene, modelDirectory);

            if (_loadedModels.TryGetValue(modelName, out var existingModel))
            {
                existingModel.Dispose();
            }

            _loadedModels[modelName] = modelAsset;
            _modelExtractionDirectories[modelName] = extractionDir;

            _logger.Information(
                "Loaded model {ModelName} from zip {ZipPath} with {MeshCount} meshes and {InstanceCount} instances",
                modelName,
                zipPath,
                modelAsset.Meshes.Count,
                modelAsset.Instances.Count
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading model {ModelName} from zip {ZipPath}", modelName, zipPath);
            CleanupExtractionDirectory(modelName);

            throw;
        }
    }

    /// <summary>
    /// Loads a shader program from vertex and fragment files and registers it with the asset manager.
    /// </summary>
    /// <param name="shaderName">The name to associate with the loaded shader.</param>
    /// <param name="vertexPath">The path to the vertex shader file.</param>
    /// <param name="fragmentPath">The path to the fragment shader file.</param>
    /// <typeparam name="TVertex">The vertex type for the shader.</typeparam>
    public void LoadShaderFromFile<TVertex>(
        string shaderName,
        string vertexPath,
        string fragmentPath,
        string[] attributesNames
    )
        where TVertex :
        unmanaged, IVertex
    {
        var fullVertexPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], vertexPath);
        var fullFragmentPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], fragmentPath);

        using var vertexStream = new FileStream(fullVertexPath, FileMode.Open, FileAccess.Read);
        using var fragmentStream = new FileStream(fullFragmentPath, FileMode.Open, FileAccess.Read);

        LoadShaderFromMemory<TVertex>(shaderName, vertexStream, fragmentStream, attributesNames);
    }

    /// <summary>
    /// Loads a shader program from a file containing combined shader source and registers it with the asset manager.
    /// The file should contain both vertex and fragment shaders separated by "#shader vertex" and "#shader fragment" markers.
    /// </summary>
    /// <param name="shaderName">The name to associate with the loaded shader.</param>
    /// <param name="shaderPath">The path to the shader file.</param>
    /// <typeparam name="TVertex">The vertex type for the shader.</typeparam>
    public void LoadShaderFromFile<TVertex>(string shaderName, string shaderPath, string[] attributesNames)
        where TVertex : unmanaged, IVertex
    {
        var fullShaderPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], shaderPath);

        using var stream = new FileStream(fullShaderPath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(stream);
        var shaderSource = reader.ReadToEnd();

        LoadShaderFromMemory<TVertex>(shaderName, shaderSource, attributesNames);
    }

    /// <summary>
    /// Loads a shader program from vertex and fragment streams and registers it with the asset manager.
    /// </summary>
    /// <param name="shaderName">The name to associate with the loaded shader.</param>
    /// <param name="vertexStream">The stream containing the vertex shader source.</param>
    /// <param name="fragmentStream">The stream containing the fragment shader source.</param>
    /// <typeparam name="TVertex">The vertex type for the shader.</typeparam>
    public void LoadShaderFromMemory<TVertex>(
        string shaderName,
        Stream vertexStream,
        Stream fragmentStream,
        string[] attributesNames
    )
        where TVertex : unmanaged, IVertex
    {
        using var reader = new StreamReader(vertexStream);

        using var fragReader = new StreamReader(fragmentStream);
        var fullShaderSource = reader.ReadToEnd() + "\n" + fragReader.ReadToEnd();

        LoadShaderFromMemory<TVertex>(shaderName, fullShaderSource, attributesNames);
    }

    /// <summary>
    /// Loads a shader program from a combined shader source string and registers it with the asset manager.
    /// The source should contain both vertex and fragment shaders separated by "#shader vertex" and "#shader fragment" markers.
    /// </summary>
    /// <param name="shaderName">The name to associate with the loaded shader.</param>
    /// <param name="shaderSource">The combined shader source string.</param>
    /// <typeparam name="TVertex">The vertex type for the shader.</typeparam>
    public void LoadShaderFromMemory<TVertex>(string shaderName, string shaderSource, string[]? attributesNames)
        where TVertex : unmanaged, IVertex
    {
        var shaders = ParseShaderSource(shaderSource);

        if (!shaders.TryGetValue(ShaderType.VertexShader, out var vertexSource) ||
            !shaders.TryGetValue(ShaderType.FragmentShader, out var fragmentSource))
        {
            throw new InvalidOperationException(
                $"Shader source must contain both vertex and fragment shaders. " +
                $"Expected format: '#shader vertex' followed by vertex shader code, " +
                $"then '#shader fragment' followed by fragment shader code."
            );
        }

        if (attributesNames == null || attributesNames.Length == 0)
        {
            // extract attribute from TVertex type
            var vertexType = typeof(TVertex);
            var properties = vertexType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var attribNames = new List<string>();

            foreach (var property in properties)
            {
                var attrib = property.GetCustomAttribute<VertexPropertyNameAttribute>();

                if (attrib != null)
                {
                    attribNames.Add(attrib.Name);
                }
            }

            attributesNames = attribNames.ToArray();
        }

        if (attributesNames == null || attributesNames.Length == 0)
        {
            throw new InvalidOperationException(
                $"Attribute names must be provided either via the attributesNames parameter " +
                $"or through VertexPropertyNameAttribute annotations on the {typeof(TVertex).Name} type."
            );
        }

        var _program = ShaderProgram.FromCode<TVertex>(
            _context.GraphicsDevice,
            vertexSource,
            fragmentSource,
            attributesNames
        );

        _logger.Information("Loaded shader {ShaderName}", shaderName);

        _shaderPrograms[shaderName] = _program;
    }

    /// <summary>
    /// Loads a texture atlas from a file.
    /// </summary>
    /// <param name="atlasName">The name of the atlas.</param>
    /// <param name="atlasPath">The path to the atlas file.</param>
    /// <param name="tileWidth">The width of each tile.</param>
    /// <param name="tileHeight">The height of each tile.</param>
    /// <param name="spacing">The spacing between tiles.</param>
    /// <param name="margin">The margin around the atlas.</param>
    public void LoadTextureAtlasFromFile(
        string atlasName,
        string atlasPath,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0
    )
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], atlasPath);

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        LoadTextureAtlasFromMemory(atlasName, stream, tileWidth, tileHeight, spacing, margin);
    }

    /// <summary>
    /// Loads a texture atlas from a stream.
    /// </summary>
    /// <param name="atlasName">The name of the atlas.</param>
    /// <param name="stream">The stream containing the atlas data.</param>
    /// <param name="tileWidth">The width of each tile.</param>
    /// <param name="tileHeight">The height of each tile.</param>
    /// <param name="spacing">The spacing between tiles.</param>
    /// <param name="margin">The margin around the atlas.</param>
    public void LoadTextureAtlasFromMemory(
        string atlasName,
        Stream stream,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0
    )
    {
        var textureName = atlasName + "_atlas";
        LoadTextureFromMemory(textureName, stream);
        var texture = _texture2Ds[textureName];
        _texture2Ds[textureName] = texture;

        var atlasDefinition = new AtlasDefinition(textureName, atlasName, tileWidth, tileHeight, margin, spacing);

        var columns = (int)((texture.Width - 2 * margin + spacing) / (float)(tileWidth + spacing));
        var rows = (int)((texture.Height - 2 * margin + spacing) / (float)(tileHeight + spacing));

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                // Calculate pixel position of this tile in the atlas
                var pixelX = margin + x * (tileWidth + spacing);
                var pixelY = margin + y * (tileHeight + spacing);

                // Convert to UV coordinates (0.0 - 1.0)
                var uvX = pixelX / (float)texture.Width;
                var uvY = pixelY / (float)texture.Height;
                var uvWidth = tileWidth / (float)texture.Width;
                var uvHeight = tileHeight / (float)texture.Height;

                var region = new AtlasRegion(
                    new(uvX, uvY),
                    new(uvWidth, uvHeight)
                );

                atlasDefinition.AddRegion(region);
            }
        }

        _textureAtlases[atlasName] = atlasDefinition;

        _logger.Information(
            "Loaded texture atlas {AtlasName} - Texture: {TextureName}, Tiles: {Columns}x{Rows}, TileSize: {TileWidth}x{TileHeight}",
            atlasName,
            textureName,
            columns,
            rows,
            tileWidth,
            tileHeight
        );
    }

    /// <summary>
    /// Loads a texture from a file and registers it with the asset manager.
    /// </summary>
    /// <param name="textureName">The name to associate with the loaded texture.</param>
    /// <param name="texturePath">The path to the texture file.</param>
    public void LoadTextureFromFile(string textureName, string texturePath)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], texturePath);

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        LoadTextureFromMemory(textureName, stream);
    }

    /// <summary>
    /// Loads a texture from a stream and registers it with the asset manager.
    /// Magenta pixels (RGB: 255, 0, 255) are automatically replaced with transparent pixels.
    /// </summary>
    /// <param name="textureName">The name to associate with the loaded texture.</param>
    /// <param name="stream">The stream containing the texture data.</param>
    public void LoadTextureFromMemory(string textureName, Stream stream)
    {
        // Read the image from the stream to process it
        using var image = Image.Load<Rgba32>(stream);

        // Replace magenta pixels with transparent ones
        ReplaceMagentaWithTransparency(image);

        // Create the texture
        var texture = new Texture2D(_context.GraphicsDevice, (uint)image.Width, (uint)image.Height);

        // Get access to the contiguous pixel memory if possible, or copy to an array
        if (!image.DangerousTryGetSinglePixelMemory(out var memory))
        {
            var pixels = new Rgba32[image.Width * image.Height];
            image.CopyPixelDataTo(pixels);
            memory = pixels.AsMemory();
        }

        // Upload data directly to the GPU
        var pixelSpan = MemoryMarshal.Cast<Rgba32, Color4b>(memory.Span);
        texture.SetData(pixelSpan, PixelFormat.Rgba);

        // Generate mipmaps
        texture.GenerateMipmaps();

        _texture2Ds[textureName] = texture;
        _logger.Information("Loaded texture {TextureName} (magenta pixels converted to transparent)", textureName);
    }

    private ModelAsset BuildModelAsset(string modelName, Scene scene, string modelDirectory)
    {
        var materialTextures = LoadMaterialTextures(modelName, scene, modelDirectory);

        var meshes = new List<ModelMeshData>(scene.MeshCount);

        for (var i = 0; i < scene.MeshCount; i++)
        {
            meshes.Add(CreateMeshData(scene.Meshes[i], materialTextures));
        }

        var instances = new List<ModelInstance>();
        CollectModelInstances(scene.RootNode, Matrix4x4.Identity, instances);

        var bounds = ComputeModelBounds(meshes, instances);

        return new(meshes, instances, bounds);
    }

    private void CollectModelInstances(Node node, Matrix4x4 parentTransform, List<ModelInstance> instances)
    {
        var nodeTransform = ToMatrix(node.Transform);
        var combined = nodeTransform * parentTransform;

        foreach (var meshIndex in node.MeshIndices)
        {
            instances.Add(new(meshIndex, combined));
        }

        foreach (var child in node.Children)
        {
            CollectModelInstances(child, combined, instances);
        }
    }

    private static BoundingBox ComputeBounds(ReadOnlySpan<VertexPositionNormalTex> vertices)
    {
        if (vertices.IsEmpty)
        {
            return new(Vector3.Zero, Vector3.Zero);
        }

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);

        foreach (var v in vertices)
        {
            min = Vector3.Min(min, v.Position);
            max = Vector3.Max(max, v.Position);
        }

        return new(min, max);
    }

    private static BoundingBox ComputeModelBounds(
        IReadOnlyList<ModelMeshData> meshes,
        IReadOnlyList<ModelInstance> instances
    )
    {
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);

        var any = false;

        foreach (var instance in instances)
        {
            var meshBounds = meshes[instance.MeshIndex].Bounds;
            var transformed = TransformBounds(meshBounds, instance.Transform);
            min = Vector3.Min(min, transformed.Min);
            max = Vector3.Max(max, transformed.Max);
            any = true;
        }

        return any ? new(min, max) : new BoundingBox(Vector3.Zero, Vector3.Zero);
    }

    private ModelMeshData CreateMeshData(Mesh mesh, IReadOnlyDictionary<int, string> materialTextures)
    {
        var vertices = new VertexPositionNormalTex[mesh.VertexCount];
        var positions = new List<Vector3>(mesh.VertexCount);

        var hasNormals = mesh.HasNormals;
        var hasTexCoords = mesh.HasTextureCoords(0);

        for (var i = 0; i < mesh.VertexCount; i++)
        {
            var pos = ToVector3(mesh.Vertices[i]);
            var norm = hasNormals ? ToVector3(mesh.Normals[i]) : Vector3.Zero;
            var uv = hasTexCoords ? mesh.TextureCoordinateChannels[0][i] : default;

            vertices[i] = new(pos, norm, ToVector2(uv));
            positions.Add(pos);
        }

        var indices = new List<uint>(mesh.FaceCount * 3);
        var indicesInt = new List<int>(mesh.FaceCount * 3);

        foreach (var face in mesh.Faces)
        {
            if (face.IndexCount != 3)
            {
                continue;
            }

            indices.Add((uint)face.Indices[0]);
            indices.Add((uint)face.Indices[1]);
            indices.Add((uint)face.Indices[2]);

            indicesInt.Add(face.Indices[0]);
            indicesInt.Add(face.Indices[1]);
            indicesInt.Add(face.Indices[2]);
        }

        var vertexBuffer = new VertexBuffer<VertexPositionNormalTex>(
            _context.GraphicsDevice,
            (uint)vertices.Length,
            (uint)indices.Count,
            ElementType.UnsignedInt,
            BufferUsage.StaticCopy,
            vertices
        );

        if (indices.Count > 0 && vertexBuffer.IndexSubset != null)
        {
            vertexBuffer.IndexSubset.SetData(CollectionsMarshal.AsSpan(indices));
        }

        var bounds = ComputeBounds(vertices);
        materialTextures.TryGetValue(mesh.MaterialIndex, out var textureKey);

        return new(
            vertexBuffer,
            (uint)indices.Count,
            mesh.MaterialIndex,
            bounds,
            textureKey,
            positions,
            indicesInt
        );
    }

    private IReadOnlyDictionary<int, string> LoadMaterialTextures(string modelName, Scene scene, string modelDirectory)
    {
        var result = new Dictionary<int, string>();

        for (var i = 0; i < scene.MaterialCount; i++)
        {
            var material = scene.Materials[i];
            var key = $"{modelName}_mat{i}";

            if (material.GetMaterialTextureCount(AssimpTextureType.Diffuse) == 0)
            {
                continue;
            }

            if (material.GetMaterialTexture(AssimpTextureType.Diffuse, 0, out var texSlot))
            {
                var texturePath = texSlot.FilePath;

                // Embedded texture (starts with '*')
                if (!string.IsNullOrEmpty(texturePath) && texturePath.StartsWith("*"))
                {
                    var index = int.Parse(texturePath[1..]);

                    if (index >= 0 && index < scene.TextureCount)
                    {
                        var embedded = scene.Textures[index];
                        LoadTextureFromMemory(key, new MemoryStream(embedded.CompressedData));
                        result[i] = key;

                        continue;
                    }
                }

                // External texture relative to model directory
                var fullPath = Path.Combine(modelDirectory, texturePath);

                if (File.Exists(fullPath))
                {
                    using var fs = File.OpenRead(fullPath);
                    _logger.Information(
                        "Loading texture for material {MaterialIndex} of {ModelName} from {TexturePath}",
                        i,
                        modelName,
                        fullPath
                    );
                    LoadTextureFromMemory(key, fs);
                    result[i] = key;
                }
            }
        }

        return result;
    }

    private void LoadMaterialFromJson(string fallbackName, string json)
    {
        var materialDto = JsonUtils.Deserialize<LillyMaterialJson>(json)
                          ?? throw new JsonException("Failed to deserialize material JSON.");

        Material material;
        if (!string.IsNullOrWhiteSpace(materialDto.BaseMaterial) &&
            _materials.TryGetValue(materialDto.BaseMaterial, out var baseMaterial))
        {
            material = baseMaterial.Clone();
        }
        else
        {
            material = new Material();
        }

        // Apply overrides (strings only when provided)
        if (!string.IsNullOrWhiteSpace(materialDto.Name))
        {
            material.Name = materialDto.Name;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.ShaderName))
        {
            material.ShaderName = materialDto.ShaderName;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.AlbedoTexture))
        {
            material.AlbedoTexture = materialDto.AlbedoTexture;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.NormalTexture))
        {
            material.NormalTexture = materialDto.NormalTexture;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.RoughnessTexture))
        {
            material.RoughnessTexture = materialDto.RoughnessTexture;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.MetallicTexture))
        {
            material.MetallicTexture = materialDto.MetallicTexture;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.EmissiveTexture))
        {
            material.EmissiveTexture = materialDto.EmissiveTexture;
        }
        if (!string.IsNullOrWhiteSpace(materialDto.AOTexture))
        {
            material.AOTexture = materialDto.AOTexture;
        }

        material.Tint = materialDto.Tint;
        material.Roughness = materialDto.Roughness;
        material.Metallic = materialDto.Metallic;
        material.EmissiveColor = materialDto.EmissiveColor;
        material.EmissiveIntensity = materialDto.EmissiveIntensity;
        material.IsTransparent = materialDto.IsTransparent;
        material.AlphaThreshold = materialDto.AlphaThreshold;
        material.CastShadows = materialDto.CastShadows;
        material.ReceiveShadows = materialDto.ReceiveShadows;
        material.TextureScale = materialDto.TextureScale;
        material.TextureOffset = materialDto.TextureOffset;

        var finalName = string.IsNullOrWhiteSpace(material.Name) ? fallbackName : material.Name;
        material.Name = finalName;

        _materials[finalName] = material;

        _logger.Information("Loaded material {MaterialName}", finalName);
    }

    private static Dictionary<ShaderType, string> ParseShaderSource(string source)
    {
        var shaders = new Dictionary<ShaderType, string>();

        using var reader = new StringReader(source);
        string? line;
        ShaderType? currentType = null;
        var currentShaderBuilder = new StringBuilder();

        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("#shader vertex", StringComparison.OrdinalIgnoreCase))
            {
                if (currentType.HasValue && currentShaderBuilder.Length > 0)
                {
                    shaders[currentType.Value] = currentShaderBuilder.ToString().Trim();
                    currentShaderBuilder.Clear();
                }
                currentType = ShaderType.VertexShader;
            }
            else if (line.StartsWith("#shader fragment", StringComparison.OrdinalIgnoreCase))
            {
                if (currentType.HasValue && currentShaderBuilder.Length > 0)
                {
                    shaders[currentType.Value] = currentShaderBuilder.ToString().Trim();
                    currentShaderBuilder.Clear();
                }
                currentType = ShaderType.FragmentShader;
            }
            else if (currentType.HasValue)
            {
                currentShaderBuilder.AppendLine(line);
            }
        }

        // Add the last shader
        if (currentType.HasValue && currentShaderBuilder.Length > 0)
        {
            shaders[currentType.Value] = currentShaderBuilder.ToString().Trim();
        }

        return shaders;
    }

    /// <summary>
    /// Replaces all magenta pixels (RGB: 255, 0, 255) with transparent pixels in the image.
    /// This is useful for handling transparency in sprite sheets or textures.
    /// </summary>
    /// <param name="image">The image to process.</param>
    private static void ReplaceMagentaWithTransparency(Image<Rgba32> image)
    {
        image.ProcessPixelRows(
            accessor =>
            {
                for (var y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);

                    for (var x = 0; x < row.Length; x++)
                    {
                        var pixel = row[x];

                        // Check if pixel is magenta (RGB: 255, 0, 255)
                        if (pixel.R == 255 && pixel.G == 0 && pixel.B == 255)
                        {
                            // Set alpha to 0 to make it transparent
                            row[x] = new(255, 0, 255, 0);
                        }
                    }
                }
            }
        );
    }

    private static Matrix4x4 ToMatrix(AssimpMatrix4x4 matrix)
        => new(
            matrix.A1,
            matrix.A2,
            matrix.A3,
            matrix.A4,
            matrix.B1,
            matrix.B2,
            matrix.B3,
            matrix.B4,
            matrix.C1,
            matrix.C2,
            matrix.C3,
            matrix.C4,
            matrix.D1,
            matrix.D2,
            matrix.D3,
            matrix.D4
        );

    private static Vector2 ToVector2(AssimpVector3D vector)
        => new(vector.X, vector.Y);

    private static Vector3 ToVector3(AssimpVector3D vector)
        => new(vector.X, vector.Y, vector.Z);

    private static BoundingBox TransformBounds(BoundingBox bounds, Matrix4x4 transform)
    {
        Span<Vector3> corners =
        [
            new(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
            new(bounds.Max.X, bounds.Min.Y, bounds.Min.Z),
            new(bounds.Min.X, bounds.Max.Y, bounds.Min.Z),
            new(bounds.Max.X, bounds.Max.Y, bounds.Min.Z),
            new(bounds.Min.X, bounds.Min.Y, bounds.Max.Z),
            new(bounds.Max.X, bounds.Min.Y, bounds.Max.Z),
            new(bounds.Min.X, bounds.Max.Y, bounds.Max.Z),
            new(bounds.Max.X, bounds.Max.Y, bounds.Max.Z)
        ];

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);

        foreach (var corner in corners)
        {
            var v = Vector3.Transform(corner, transform);
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        return new(min, max);
    }

    private string CreateExtractionDirectory(string modelName)
    {
        CleanupExtractionDirectory(modelName);

        var baseDir = Path.Combine(Path.GetTempPath(), "LillyModels");
        Directory.CreateDirectory(baseDir);

        var dir = Path.Combine(baseDir, $"{modelName}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        return dir;
    }

    private void CleanupExtractionDirectory(string modelName)
    {
        if (_modelExtractionDirectories.TryGetValue(modelName, out var dir))
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to cleanup extraction directory for model {ModelName} at {Dir}", modelName, dir);
            }

            _modelExtractionDirectories.Remove(modelName);
        }
    }
}
