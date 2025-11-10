using System.Runtime.InteropServices.ComTypes;

namespace Lilly.Engine.Interfaces.Services;

public interface IAssetManager
{

    TTexture GetWhiteTexture<TTexture>() where TTexture : class;

    void LoadFontFromFile(string fontName, string fontPath);
    void LoadFontFromMemory(string fontName, Stream stream);

    void LoadTextureFromFile(string textureName, string texturePath);
    void LoadTextureFromMemory(string textureName, Stream stream);


    TFont GetFont<TFont>(string fontName, int size) where TFont : class;
    TTexture GetTexture<TTexture>(string textureName) where TTexture : class;
}
