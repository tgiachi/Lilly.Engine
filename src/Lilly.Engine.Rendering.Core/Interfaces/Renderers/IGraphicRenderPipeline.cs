using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

namespace Lilly.Engine.Rendering.Core.Interfaces.Renderers;

public interface IGraphicRenderPipeline
{
    ReadOnlySpan<IRenderLayerSystem> RenderLayers { get; }
    void AddGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject;

    TRenderSystem? GetRenderLayerSystem<TRenderSystem>() where TRenderSystem : class, IRenderLayerSystem;

    void Initialize();

    void RemoveGameObject<TGameObject>(TGameObject gameObject) where TGameObject : IGameObject;

    void Render(GameTime gameTime);

    void Update(GameTime gameTime);

    void ViewportResize(int width, int height);
}
