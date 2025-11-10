using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Represents a game object that renders an image using a texture key.
/// </summary>
public class ImageGameObject : BaseGameObject2D
{
    /// <summary>
    /// Gets or sets the key of the texture to be drawn.
    /// </summary>
    public string TextureKey { get; set; }

    protected override IEnumerable<RenderCommand> Draw(GameTime gameTime)
    {
        if (string.IsNullOrEmpty(TextureKey))
        {
            yield break;
        }

        yield return DrawTexture(
            TextureKey,
            color: Color4b.White
        );
    }
}
