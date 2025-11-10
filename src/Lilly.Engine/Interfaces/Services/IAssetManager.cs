using System.Runtime.InteropServices.ComTypes;
using TrippyGL;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Defines the contract for managing game assets such as textures, fonts, and shaders.
/// </summary>
public interface IAssetManager
{
    TTexture GetWhiteTexture<TTexture>() where TTexture : class;

    void LoadFontFromFile(string fontName, string fontPath);
    void LoadFontFromMemory(string fontName, Stream stream);

    void LoadTextureFromFile(string textureName, string texturePath);
    void LoadTextureFromMemory(string textureName, Stream stream);

    void LoadShaderFromFile<TVertex>(string shaderName, string vertexPath, string fragmentPath)
        where TVertex :
        unmanaged, IVertex;

    void LoadShaderFromMemory<TVertex>(string shaderName, Stream vertexStream, Stream fragmentStream)
        where TVertex : unmanaged, IVertex;

    ShaderProgram GetShaderProgram(string shaderName);

    TFont GetFont<TFont>(string fontName, int size) where TFont : class;
    TTexture GetTexture<TTexture>(string textureName) where TTexture : class;
}
