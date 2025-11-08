using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Collections;
using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Interfaces.GameObjects;
using Lilly.Engine.Rendering.Core.Primitives;

namespace Lilly.Engine.Rendering.Core.Base.GameObjects;

public abstract class BaseGameObject2D : IGameObject2D
{
    public IGameObject? Parent { get; set; }
    public GameObjectCollection<IGameObject> Children { get; } = new();
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ushort Order { get; set; }
    public Transform2D Transform { get; } = new();
    public bool IsVisible { get; set; } = true;
    public int Layer { get; set; }

    public void Render(GameTime gameTime, ref List<RenderCommand> renderCommands)
    {
        if (!IsVisible) return;

        Draw(gameTime, ref renderCommands);

        foreach (var child in Children)
        {
            child.Render(gameTime, ref renderCommands);
        }
    }

    public abstract void Draw(GameTime gameTime, ref List<RenderCommand> renderCommands);
}
