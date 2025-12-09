using System.Reflection;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Provides extension methods for loading assets from embedded resources into the asset manager.
/// </summary>
public static class AssetManagerServiceExtensions
{
    /// <summary>
    /// Loads a shader from an embedded resource and registers it with the asset manager.
    /// </summary>
    /// <typeparam name="TVertex">The vertex type for the shader.</typeparam>
    /// <param name="shaderName">The name to register the shader under.</param>
    /// <param name="resourcePath">The path to the embedded resource.</param>
    /// <param name="attributeNames">The attribute names for the shader.</param>
    /// <param name="assembly">The assembly containing the resource.</param>
    public static void LoadShaderFromResource<TVertex>(
        this IAssetManager assetManager,
        string shaderName,
        string resourcePath,
        string[] attributeNames,
        Assembly assembly
    )
        where TVertex : unmanaged, IVertex
    {
        using var shader = ResourceUtils.GetEmbeddedResourceStream(assembly, resourcePath);
        var shaderContent = new StreamReader(shader).ReadToEnd();

        if (shader == null)
        {
            throw new InvalidOperationException($"Resource '{resourcePath}' not found in assembly '{assembly.FullName}'.");
        }

        assetManager.LoadShaderFromMemory<TVertex>(shaderName, shaderContent, attributeNames);
    }

    /// <summary>
    /// Loads a texture atlas from an embedded resource and registers it with the asset manager.
    /// </summary>
    /// <param name="atlasName">The name to register the atlas under.</param>
    /// <param name="resourcePath">The path to the embedded resource.</param>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="tileWidth">The width of each tile in the atlas.</param>
    /// <param name="tileHeight">The height of each tile in the atlas.</param>
    /// <param name="spacing">The spacing between tiles.</param>
    /// <param name="margin">The margin around the atlas.</param>
    public static void LoadTextureAtlasFromResource(
        this IAssetManager assetManager,
        string atlasName,
        string resourcePath,
        Assembly assembly,
        int tileWidth,
        int tileHeight,
        int spacing = 0,
        int margin = 0
    )
    {
        using var textureStream = ResourceUtils.GetEmbeddedResourceStream(assembly, resourcePath);

        if (textureStream == null)
        {
            throw new InvalidOperationException($"Resource '{resourcePath}' not found in assembly '{assembly.FullName}'.");
        }

        assetManager.LoadTextureAtlasFromMemory(atlasName, textureStream, tileWidth, tileHeight, spacing, margin);
    }

    /// <summary>
    /// Loads a texture from an embedded resource and registers it with the asset manager.
    /// </summary>
    /// <param name="textureName">The name to register the texture under.</param>
    /// <param name="resourcePath">The path to the embedded resource.</param>
    /// <param name="assembly">The assembly containing the resource.</param>
    public static void LoadTextureFromResource(
        this IAssetManager assetManager,
        string textureName,
        string resourcePath,
        Assembly assembly
    )
    {
        using var textureStream = ResourceUtils.GetEmbeddedResourceStream(assembly, resourcePath);

        if (textureStream == null)
        {
            throw new InvalidOperationException($"Resource '{resourcePath}' not found in assembly '{assembly.FullName}'.");
        }

        assetManager.LoadTextureFromMemory(textureName, textureStream);
    }
}
