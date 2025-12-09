using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.GameObjects.TwoD;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Context;
using Lilly.Rendering.Core.Interfaces.Services;

namespace Lilly.Voxel.Plugin.GameObjects;

public class CrosshairGameObject : TextureGameObject
{
    private readonly RenderContext _renderContext;

    public CrosshairGameObject(
        IGameObjectManager gameObjectManager,
        RenderContext renderContext,
        IAssetManager assetManager
    ) : base(gameObjectManager, assetManager)
    {
        _renderContext = renderContext;
        TextureName = "crosshair";
        Transform.Scale = new(4, 4);
    }

    public override void Update(GameTime gameTime)
    {
        var width = _renderContext.GraphicsDevice.Viewport.Width;
        var height = _renderContext.GraphicsDevice.Viewport.Height;

        var textureSize = GetTextureSize();
        Transform.Position = new(width / 2f - textureSize.X / 2, height / 2f - textureSize.Y / 2);

        //Size = GetTextureSize();

        base.Update(gameTime);
    }
}
