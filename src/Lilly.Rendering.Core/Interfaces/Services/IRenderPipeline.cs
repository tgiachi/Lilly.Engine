using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Pipeline;

namespace Lilly.Rendering.Core.Interfaces.Services;

public interface IRenderPipeline : IGameObjectManager
{

    IEnumerable<IRenderLayer> RenderLayers { get; }
    void AddRenderLayer<TRenderLayer>(TRenderLayer renderLayer) where TRenderLayer : IRenderLayer;

    void Render(GameTime gameTime);

    void Update(GameTime gameTime);
}
