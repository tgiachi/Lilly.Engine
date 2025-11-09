using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Renderers;

public interface IGraphicRenderPipeline
{
    void AddGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject;

    void RemoveGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject;

    ReadOnlySpan<IRenderLayerSystem> RenderLayers { get; }

    void Render(GameTime gameTime);

    void Update(GameTime gameTime);


    void ViewportResize(int width, int height);

    void Initialize();

}
