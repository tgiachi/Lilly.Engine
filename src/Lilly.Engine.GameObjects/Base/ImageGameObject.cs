using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Base.GameObjects;
using Lilly.Engine.Rendering.Core.Commands;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

public class ImageGameObject : BaseGameObject2D
{
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
