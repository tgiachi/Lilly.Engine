namespace Lilly.Rendering.Core.Interfaces.Entities;

public interface IGameObject
{
    uint Id { get; set; }
    string Name { get; set; }
    uint ZIndex { get; set; }
    bool IsActive { get; set; }
    IGameObject? Parent { get; set; }
    IEnumerable<IGameObject> Children { get; }

}
