using FontStashSharp;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Utils;
using Serilog;
using Silk.NET.OpenGL;
using TrippyGL;
using TrippyGL.ImageSharp;

namespace Lilly.Engine.Services;

public class AssetManager : IAssetManager
{
    private readonly int[] defaultSizes = [8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 40, 48, 64, 72, 96, 128, 144, 192, 256];

    private readonly ILogger _logger = Log.ForContext<AssetManager>();
    private readonly DirectoriesConfig _directoriesConfig;
    private readonly RenderContext _context;

    private readonly Dictionary<string, FontSystem> _fontSystems = new();
    private readonly Dictionary<string, DynamicSpriteFont> _dynamicSpriteFonts = new();

    private readonly Dictionary<string, Texture2D> _texture2Ds = new();
    private readonly Dictionary<string, ShaderProgram> _shaderPrograms = new();

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
            _whiteTexture = new Texture2D(_context.GraphicsDevice, 1, 1);

            // Set the single pixel to white (RGBA: 255, 255, 255, 255)
            var whitePixel = new Color4b(255, 255, 255, 255);
            ReadOnlySpan<Color4b> pixelData = [whitePixel];
            _whiteTexture.SetData<Color4b>(pixelData, PixelFormat.Rgba);

            _logger.Debug("Created white texture (1x1)");
        }

        return _whiteTexture as TTexture ??
               throw new InvalidOperationException($"Cannot cast white texture to {typeof(TTexture).Name}");
    }

    public void LoadFontFromFile(string fontName, string fontPath)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], fontPath);

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        LoadFontFromMemory(fontName, stream);
    }

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

    public void LoadTextureFromFile(string textureName, string texturePath)
    {
        var fullPath = Path.Combine(_directoriesConfig[DirectoryType.Assets], texturePath);

        using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

        LoadTextureFromMemory(textureName, stream);
    }

    public void LoadTextureFromMemory(string textureName, Stream stream)
    {
        var texture = Texture2DExtensions.FromStream(_context.GraphicsDevice, stream, true);
        _texture2Ds[textureName] = texture;
        _logger.Information("Loaded texture {TextureName}", textureName);
    }

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

    public ShaderProgram GetShaderProgram(string shaderName)
    {
        return _shaderPrograms.TryGetValue(shaderName, out var shaderProgram)
                   ? shaderProgram
                   : throw new InvalidOperationException($"Shader '{shaderName}' is not loaded.");
    }

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

    public TTexture GetTexture<TTexture>(string textureName) where TTexture : class
    {
        if (_texture2Ds.TryGetValue(textureName, out var texture))
        {
            return texture as TTexture;
        }

        throw new InvalidOperationException($"Texture '{textureName}' is not loaded.");
    }
}
