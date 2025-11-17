using System.Diagnostics;
using System.Reflection;
using FontStashSharp;
using Lilly.Engine.Attributes;
using Lilly.Engine.Core.Data.Directories;
using Lilly.Engine.Core.Enums;
using Lilly.Engine.Rendering.Core.Contexts;
using Lilly.Engine.Rendering.Core.Interfaces.Services;
using Lilly.Engine.Rendering.Core.Utils;
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
