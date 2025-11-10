using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Interfaces.EngineLayers;

public interface IRenderLayerSystem
{
    RenderLayer Layer { get; }

    string Name { get; }

    /// <summary>
    /// Gets the set of command types that this layer system can process.
    /// This enables efficient command filtering in the rendering pipeline.
    /// </summary>
    IReadOnlySet<RenderCommandType> SupportedCommandTypes { get; }

    void Add(IGameObject gameObject);

    bool CanAddOrRemove(IGameObject gameObject);

    void Clear();

    List<RenderCommand> CollectRenderCommands(GameTime gameTime);

    IEnumerable<IGameObject> GetAllGameObjects();

    void Initialize();

    void OnViewportResize(int width, int height);

    void ProcessRenderCommands(ref List<RenderCommand> renderCommands);

    void Remove(IGameObject gameObject);

    void Update(GameTime gameTime);
}
