using FontStashSharp;
using Lilly.Engine.Audio;
using Lilly.Engine.Data.Assets;
using Lilly.Engine.Data.Atlas;
using TrippyGL;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Defines the contract for managing game assets such as textures, fonts, and shaders.
/// </summary>
public interface IAssetManager
{
    /// <summary>
    /// Creates a vertex buffer from vertex data.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="vertices">The vertex data array.</param>
    /// <param name="usage">The buffer usage hint.</param>
    /// <returns>The created vertex buffer.</returns>
    VertexBuffer<TVertex> CreateVertexBuffer<TVertex>(TVertex[] vertices, BufferUsage usage = BufferUsage.StaticCopy)
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Gets a specific region from a loaded texture atlas.
    /// </summary>
    /// <param name="atlasName">The name of the atlas.</param>
    /// <param name="tileIndex">The index of the tile in the atlas.</param>
    /// <returns>The atlas region with UV coordinates and size.</returns>
    AtlasRegion GetAtlasRegion(string atlasName, int tileIndex);

    /// <summary>
    /// Gets a specific region from a loaded texture atlas by its x and y indices.
    /// </summary>
    /// <param name="atlasName"></param>
    /// <param name="xIndex"></param>
    /// <param name="yIndex"></param>
    /// <returns></returns>
    AtlasRegion GetAtlasRegion(string atlasName, int xIndex, int yIndex);

    /// <summary>
    /// Gets the font by name and size.
    /// </summary>
    /// <param name="fontName">The name of the font.</param>
    /// <param name="size">The size of the font.</param>
    /// <returns>The font.</returns>
    DynamicSpriteFont GetFont(string fontName, int size);

    /// <summary>
    /// Retrieves a previously loaded 3D model.
    /// </summary>
    /// <param name="modelName"></param>
    /// <returns></returns>
    ModelAsset GetModel(string modelName);

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
    /// Gets the texture handle by name.
    /// </summary>
    /// <param name="textureName"></param>
    /// <returns></returns>
    uint GetTextureHandle(string textureName);

    /// <summary>
    /// Gets the white texture.
    /// </summary>
    TTexture GetWhiteTexture<TTexture>() where TTexture : class;

    /// <summary>
    ///  Gets the white texture.
    /// </summary>
    /// <returns></returns>
    Texture2D GetWhiteTexture();

    /// <summary>
    ///  Gets the black texture.
    /// </summary>
    /// <returns></returns>
    Texture2D GetBlackTexture();

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
    /// Loads a 3D model from a file.
    /// </summary>
    /// <param name="modelName"></param>
    /// <param name="modelPath"></param>
    void LoadModelFromFile(string modelName, string modelPath);

    /// <summary>
    /// Loads a sound effect from a file located under the assets directory.
    /// </summary>
    /// <param name="soundName">Key used to store the sound.</param>
    /// <param name="soundPath">Path relative to assets.</param>
    /// <param name="audioType">Audio format hint.</param>
    void LoadSoundFromFile(string soundName, string soundPath, AudioType audioType = AudioType.Ogg);

    /// <summary>
    /// Loads a sound effect from a stream.
    /// </summary>
    /// <param name="soundName">Key used to store the sound.</param>
    /// <param name="stream">Stream containing audio data.</param>
    /// <param name="audioType">Audio format hint.</param>
    void LoadSoundFromStream(string soundName, Stream stream, AudioType audioType = AudioType.Ogg);

    /// <summary>
    /// Loads a 3D model from a zip archive located under the assets directory.
    /// </summary>
    /// <param name="modelName">Key to store the model.</param>
    /// <param name="zipPath">Zip path relative to assets.</param>
    /// <param name="modelPathInZip">Relative path to the model file inside the zip.</param>
    void LoadModelFromZip(string modelName, string zipPath, string modelPathInZip);

    /// <summary>
    /// Loads a shader from files.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="vertexPath">The path to the vertex shader file.</param>
    /// <param name="fragmentPath">The path to the fragment shader file.</param>
    void LoadShaderFromFile<TVertex>(string shaderName, string vertexPath, string fragmentPath, string[] attributesNames)
        where TVertex :
        unmanaged, IVertex;

    /// <summary>
    /// Loads a shader from a file containing combined shader source.
    /// The file should contain both vertex and fragment shaders separated by "#shader vertex" and "#shader fragment" markers.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="shaderPath">The path to the shader file.</param>
    void LoadShaderFromFile<TVertex>(string shaderName, string shaderPath, string[] attributesNames)
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Loads a shader from memory.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="vertexStream">The stream containing the vertex shader data.</param>
    /// <param name="fragmentStream">The stream containing the fragment shader data.</param>
    void LoadShaderFromMemory<TVertex>(
        string shaderName,
        Stream vertexStream,
        Stream fragmentStream,
        string[] attributesNames
    )
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Loads a shader from a combined shader source string.
    /// The source should contain both vertex and fragment shaders separated by "#shader vertex" and "#shader fragment" markers.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type.</typeparam>
    /// <param name="shaderName">The name of the shader.</param>
    /// <param name="shaderSource">The combined shader source string.</param>
    void LoadShaderFromMemory<TVertex>(string shaderName, string shaderSource, string[] attributesNames)
        where TVertex : unmanaged, IVertex;

    /// <summary>
    /// Loads a texture atlas from a file.
    /// </summary>
    /// <param name="atlasName"></param>
    /// <param name="atlasPath"></param>
    /// <param name="tileWidth"></param>
    /// <param name="tileHeight"></param>
    /// <param name="spacing"></param>
    /// <param name="margin"></param>
    void LoadTextureAtlasFromFile(
        string atlasName,
        string atlasPath,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0
    );

    /// <summary>
    /// Loads a texture atlas from memory.
    /// </summary>
    /// <param name="atlasName"></param>
    /// <param name="stream"></param>
    /// <param name="tileWidth"></param>
    /// <param name="tileHeight"></param>
    /// <param name="spacing"></param>
    /// <param name="margin"></param>
    void LoadTextureAtlasFromMemory(
        string atlasName,
        Stream stream,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0
    );

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
}
