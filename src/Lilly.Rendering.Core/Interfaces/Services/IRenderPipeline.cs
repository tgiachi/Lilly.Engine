using Lilly.Rendering.Core.Data.Game;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Pipeline;

namespace Lilly.Rendering.Core.Interfaces.Services;

public interface IRenderPipeline
{

    IEnumerable<IRenderLayer> RenderLayers { get; }
    void AddRenderLayer<TRenderLayer>(TRenderLayer renderLayer) where TRenderLayer : IRenderLayer;

    void Update(GameTime gameTime);

    void Render(GameTime gameTime);

    void AddGameObject<TEntity>(TEntity entity) where TEntity : IGameObject;

    void RemoveGameObject<TEntity>(TEntity entity) where TEntity : IGameObject;
}
