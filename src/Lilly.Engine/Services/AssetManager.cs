using System.Reflection;
using Assimp;
using Assimp.Configs;
using FontStashSharp;
using Lilly.Engine.Attributes;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Data.Atlas;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Utils;
using Lilly.Rendering.Core.Context;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TrippyGL;
using TrippyGL.ImageSharp;

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

    private readonly Dictionary<string, FontSystem> _fontSystems = new();
    private readonly Dictionary<string, DynamicSpriteFont> _dynamicSpriteFonts = new();

    private readonly Dictionary<string, Texture2D> _texture2Ds = new();
    private readonly Dictionary<string, ShaderProgram> _shaderPrograms = new();

    private readonly Dictionary<string, AtlasDefinition> _textureAtlases = new();

    private readonly AssimpContext _assimpContext = new();

    private readonly PostProcessSteps _defaultPostProcessSteps = PostProcessSteps.Triangulate |
                                                                 PostProcessSteps.CalculateTangentSpace |
                                                                 PostProcessSteps.JoinIdenticalVertices |
                                                                 PostProcessSteps.FlipUVs;


    private readonly Dictionary<string, Scene> _loadedModels = new();

    private static Dictionary<ShaderType, string> ParseShaderSource(string source)
    {
        var shaders = new Dictionary<ShaderType, string>();

        // Split by shader type markers
        var lines = source.Split('\n');
        ShaderType? currentType = null;
        var currentShader = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("#shader vertex", StringComparison.OrdinalIgnoreCase))
            {
                if (currentType.HasValue && currentShader.Count > 0)
                {
                    shaders[currentType.Value] = string.Join('\n', currentShader);
                }
                currentType = ShaderType.VertexShader;
                currentShader.Clear();
            }
            else if (line.StartsWith("#shader fragment", StringComparison.OrdinalIgnoreCase))
            {
                if (currentType.HasValue && currentShader.Count > 0)
                {
                    shaders[currentType.Value] = string.Join('\n', currentShader);
                }
                currentType = ShaderType.FragmentShader;
                currentShader.Clear();
            }
            else if (currentType.HasValue)
            {
                currentShader.Add(line);
            }
        }

        // Add the last shader
        if (currentType.HasValue && currentShader.Count > 0)
        {
            shaders[currentType.Value] = string.Join('\n', currentShader);
        }

        return shaders;
    }

    // Cached white texture (1x1 white pixel) for drawing colored rectangles and fallback
    private Texture2D? _whiteTexture;

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
    public AssetManager(DirectoriesConfig directoriesConfig, RenderContext context)
    {
        _directoriesConfig = directoriesConfig;
        _context = context;
        GetWhiteTexture<Texture2D>();
        _texture2Ds[DefaultTextures.WhiteTextureKey] = _whiteTexture!;
        _assimpContext.SetConfig(new NormalSmoothingAngleConfig(66.0f));

        AssimpNet.Platform.Init();
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
                    new Vector2D<float>(uvX, uvY),
                    new Vector2D<float>(uvWidth, uvHeight)
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
    {
        return _texture2Ds.TryGetValue(textureName, out var texture)
                   ? texture.Handle
                   : throw new InvalidOperationException($"Texture '{textureName}' is not loaded.");
    }

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

        if (!shaders.TryGetValue(ShaderType.VertexShader, out string? vertexSource) ||
            !shaders.TryGetValue(ShaderType.FragmentShader, out string? fragmentSource))
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
                            row[x] = new Rgba32(255, 0, 255, 0);
                        }
                    }
                }
            }
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

        // Save the processed image to a memory stream and load it as texture
        using var processedStream = new MemoryStream();
        image.SaveAsPng(processedStream);
        processedStream.Seek(0, SeekOrigin.Begin);

        var texture = Texture2DExtensions.FromStream(_context.GraphicsDevice, processedStream, true);
        _texture2Ds[textureName] = texture;
        _logger.Information("Loaded texture {TextureName} (magenta pixels converted to transparent)", textureName);
    }

    public void LoadModelFromFile(string modelName, string modelPath)
    {
        var model = _assimpContext.ImportFile(
            Path.Combine(_directoriesConfig[DirectoryType.Assets], modelPath),
            _defaultPostProcessSteps
        );


        _loadedModels[modelName] = model;

        _logger.Information("Loaded model {ModelName} with {MeshCount} meshes", modelName, model.MeshCount);


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
        GC.SuppressFinalize(this);
    }
}
