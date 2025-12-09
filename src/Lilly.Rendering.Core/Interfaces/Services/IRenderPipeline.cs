using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Pipeline;

namespace Lilly.Rendering.Core.Interfaces.Services;

public interface IRenderPipeline : IGameObjectManager
{
    delegate void GameObjectAddedHandler(IGameObject gameObject);

    delegate void GameObjectRemovedHandler(IGameObject gameObject);

    event GameObjectAddedHandler GameObjectAdded;

    event GameObjectRemovedHandler GameObjectRemoved;

    IEnumerable<IRenderLayer> RenderLayers { get; }
    void AddRenderLayer<TRenderLayer>(TRenderLayer renderLayer) where TRenderLayer : IRenderLayer;

    void Render(GameTime gameTime);

    void Update(GameTime gameTime);
}
