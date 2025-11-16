using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Interfaces.Services;

/// <summary>
/// Defines the contract for managing game assets such as textures, fonts, and shaders.
/// </summary>
public interface IAssetManager
{
    /// <summary>
    /// Gets the font by name and size.
    /// </summary>
    /// <typeparam name="TFont">The font type.</typeparam>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="size">The size of the font.</param>
    /// <returns>The font.</returns>
    TFont GetFont<TFont>(string fontName, int size) where TFont : class;

    /// <summary>
    /// Gets the shader program by name.
    /// </summary>
    /// <param name="shaderName">The name of the shader.</param>
    /// <returns>The shader program.</returns>
    ShaderProgram GetShaderProgram(string shaderName);



    /// <summary>
    /// Gets the texture by name.
    /// </summary>
    /// <typeparam name="TTexture">The texture type.</typeparam>
    /// <param name="textureName">The name of the texture.</param>
    /// <returns>The texture.</returns>
    TTexture GetTexture<TTexture>(string textureName) where TTexture : class;

    /// <summary>
    ///  Gets the texture handle by name.
    /// </summary>
    /// <param name="textureName"></param>
    /// <returns></returns>
    uint GetTextureHandle(string textureName);

    /// <summary>
    /// Gets the white texture.
    /// </summary>
    TTexture GetWhiteTexture<TTexture>() where TTexture : class;

    /// <summary>
    /// Loads a font from a file.
    /// </summary>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="fontPath">The path to the font file.</param>
    void LoadFontFromFile(string fontName, string fontPath);

    /// <summary>
    /// Loads a font from memory.
    /// </summary>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="stream">The stream containing the font data.</param>
    void LoadFontFromMemory(string fontName, Stream stream);

    /// <summary>
    /// Loads a shader from files.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="vertexPath">The path to the vertex shader file.</param>
    /// <param name="fragmentPath">The path to the fragment shader file.</param>
    void LoadShaderFromFile<TVertex>(string shaderName, string vertexPath, string fragmentPath)
        where TVertex :
        unmanaged, IVertex;

    /// <summary>
    /// Loads a shader from a file containing combined shader source.
    /// The file should contain both vertex and fragment shaders separated by "#shader vertex" and "#shader fragment" markers.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="shaderPath">The path to the shader file.</param>
    void LoadShaderFromFile<TVertex>(string shaderName, string shaderPath)
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Loads a shader from memory.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="vertexStream">The stream containing the vertex shader data.</param>
    /// <param name="fragmentStream">The stream containing the fragment shader data.</param>
    void LoadShaderFromMemory<TVertex>(string shaderName, Stream vertexStream, Stream fragmentStream)
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Loads a shader from a combined shader source string.
    /// The source should contain both vertex and fragment shaders separated by "#shader vertex" and "#shader fragment" markers.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="shaderSource">The combined shader source string.</param>
    void LoadShaderFromMemory<TVertex>(string shaderName, string shaderSource)
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Loads a texture from a file.
    /// </summary>
    /// <param name="textureName">The name of the texture.</param>
    /// <param name="texturePath">The path to the texture file.</param>
    void LoadTextureFromFile(string textureName, string texturePath);

    /// <summary>
    /// Loads a texture from memory.
    /// </summary>
    /// <param name="textureName">The name to associate with the loaded texture.</param>
    /// <param name="stream">The stream containing the texture data.</param>
    void LoadTextureFromMemory(string textureName, Stream stream);

    /// <summary>
    /// Creates a vertex buffer from vertex data.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="vertices">The vertex data array.</param>
    /// <param name="usage">The buffer usage hint.</param>
    /// <returns>The created vertex buffer.</returns>
    VertexBuffer<TVertex> CreateVertexBuffer<TVertex>(TVertex[] vertices, BufferUsage usage = BufferUsage.StaticCopy)
        where TVertex : unmanaged, IVertex;
}
