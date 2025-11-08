using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;

public interface IRenderLayerSystem
{
    RenderLayer Layer { get; }

    string Name { get; }

    void Initialize();

    void Update(GameTime gameTime);


    void Add(IGameObject gameObject);

    void Remove(IGameObject gameObject);

    void Clear();

    IEnumerable<IGameObject> GetAllGameObjects();

    bool CanAddOrRemove(IGameObject gameObject);

    void OnViewportResize(int width, int height);

    List<RenderCommand> CollectRenderCommands(GameTime gameTime);

    void ProcessRenderCommands(ref List<RenderCommand> renderCommands);

}
