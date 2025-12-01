using System.Reflection;
using Lilly.Engine.Core.Utils;
using Lilly.Engine.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.Extensions;

public static class AssetManagerServiceExtensions
{
    extension(IAssetManager assetManager)
    {
        public void LoadShaderFromResource<TVertex>(
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
                throw new InvalidOperationException(
                    $"Resource '{resourcePath}' not found in assembly '{assembly.FullName}'."
                );
            }

            assetManager.LoadShaderFromMemory<TVertex>(shaderName, shaderContent, attributeNames);
        }

        public void LoadTextureFromResource(
            string textureName,
            string resourcePath,
            Assembly assembly
        )
        {
            using var textureStream = ResourceUtils.GetEmbeddedResourceStream(assembly, resourcePath);

            if (textureStream == null)
            {
                throw new InvalidOperationException(
                    $"Resource '{resourcePath}' not found in assembly '{assembly.FullName}'."
                );
            }

            assetManager.LoadTextureFromMemory(textureName, textureStream);
        }

        public void LoadTextureAtlasFromResource(
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
                throw new InvalidOperationException(
                    $"Resource '{resourcePath}' not found in assembly '{assembly.FullName}'."
                );
            }

            assetManager.LoadTextureAtlasFromMemory(atlasName, textureStream, tileWidth, tileHeight, spacing, margin);
        }
    }
}
