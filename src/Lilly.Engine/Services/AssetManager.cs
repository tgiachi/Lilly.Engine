using System.Diagnostics;
using FontStashSharp;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Interfaces.Shaders;
using Lilly.Engine.Rendering.Core.Utils;
using Lilly.Engine.Shaders;
using Serilog;
using Silk.NET.OpenGL;
using TrippyGL;
using TrippyGL.ImageSharp;

namespace Lilly.Engine.Services;

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

    private readonly Dictionary<string, ILillyShader> _lillyShaders = new();

    // Cached white texture (1x1 white pixel) for drawing colored rectangles and fallback
    private Texture2D? _whiteTexture;

    private readonly FontSystemSettings _defaultFontSettings = new()
    {
        FontResolutionFactor = 2,
        KernelWidth = 2,
        KernelHeight = 2
    };

    public AssetManager(DirectoriesConfig directoriesConfig, RenderContext context)
    {
        _directoriesConfig = directoriesConfig;
        _context = context;
        GetWhiteTexture<Texture2D>();
        _texture2Ds[DefaultTextures.WhiteTextureKey] = _whiteTexture!;
    }

    /// <summary>
    /// Retrieves a font with the specified name and size.
    /// </summary>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="size">The size of the font.</param>
    /// <typeparam name="TFont">The type of the font.</typeparam>
    /// <returns>The font instance.</returns>
    public TFont GetFont<TFont>(string fontName, int size) where TFont : class
    {
        var key = $"{fontName}_{size}";

        if (_dynamicSpriteFonts.TryGetValue(key, out var font))
        {
            return font as TFont;
        }

        if (_fontSystems.TryGetValue(fontName, out var fontSystem))
        {
            var dynamicFont = fontSystem.GetFont(size);
            _dynamicSpriteFonts[key] = dynamicFont;

            return dynamicFont as TFont;
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

    public void LoadLillyShaderFromFile(string name, string fileName)
    {
        var fullFilePath = Path.Combine(_directoriesConfig[DirectoryType.Assets], fileName);

        using var stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read);

        LoadLillyShaderFromStream(name, stream);
    }

    public void LoadLillyShaderFromStream(string name, Stream stream)
    {
        var stopWatch = Stopwatch.GetTimestamp();
        var streamContent = new StreamReader(stream).ReadToEnd();

        var lillyShader = new OpenGlLillyShader(_context.GraphicsDevice.GL, name);

        lillyShader.CompileAndLink(streamContent);

        _lillyShaders[name] = lillyShader;

        var endStopWatch = Stopwatch.GetTimestamp();
        _logger.Information("Loaded Lilly shader {ShaderName} in {ElapsedTime}", name, Stopwatch.GetElapsedTime(stopWatch));
    }

    public ILillyShader GetLillyShader(string shaderName)
        => throw new NotImplementedException();

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

            _logger.Debug("  - Added size {FontSize}", size);
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
    public void LoadShaderFromFile<TVertex>(string shaderName, string vertexPath, string fragmentPath)
        where TVertex :
        unmanaged, IVertex
    {
        var fullVertexPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], vertexPath);
        var fullFragmentPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], fragmentPath);

        using var vertexStream = new FileStream(fullVertexPath, FileMode.Open, FileAccess.Read);
        using var fragmentStream = new FileStream(fullFragmentPath, FileMode.Open, FileAccess.Read);

        LoadShaderFromMemory<TVertex>(shaderName, vertexStream, fragmentStream);
    }

    /// <summary>
    /// Loads a shader program from vertex and fragment streams and registers it with the asset manager.
    /// </summary>
    /// <param name="shaderName">The name to associate with the loaded shader.</param>
    /// <param name="vertexStream">The stream containing the vertex shader source.</param>
    /// <param name="fragmentStream">The stream containing the fragment shader source.</param>
    /// <typeparam name="TVertex">The vertex type for the shader.</typeparam>
    public void LoadShaderFromMemory<TVertex>(string shaderName, Stream vertexStream, Stream fragmentStream)
        where TVertex : unmanaged, IVertex
    {
        using var reader = new StreamReader(vertexStream);
        var vertexSource = reader.ReadToEnd();

        using var fragReader = new StreamReader(fragmentStream);
        var fragmentSource = fragReader.ReadToEnd();

        var _program = ShaderProgram.FromCode<TVertex>(_context.GraphicsDevice, vertexSource, fragmentSource);

        _logger.Information("Loaded shader {ShaderName}", shaderName);

        _shaderPrograms[shaderName] = _program;
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
    /// </summary>
    /// <param name="textureName">The name to associate with the loaded texture.</param>
    /// <param name="stream">The stream containing the texture data.</param>
    public void LoadTextureFromMemory(string textureName, Stream stream)
    {
        var texture = Texture2DExtensions.FromStream(_context.GraphicsDevice, stream, true);
        _texture2Ds[textureName] = texture;
        _logger.Information("Loaded texture {TextureName}", textureName);
    }

    public void Dispose()
    {
        _whiteTexture?.Dispose();

        foreach (var shaderProgram in _shaderPrograms.Values)
        {
            shaderProgram.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
