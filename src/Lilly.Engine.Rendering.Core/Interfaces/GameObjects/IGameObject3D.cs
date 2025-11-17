using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Interfaces.GameObjects;

/// <summary>
/// Represents a 3D game object with spatial transformation and rendering properties.
/// </summary>
public interface IGameObject3D : IGameObject
{
    Transform3D Transform { get; }
    bool IsVisible { get; set; }
    int Layer { get; set; }
    void Draw(ICamera3D camera, GameTime gameTime);

    bool IgnoreFrustumCulling { get; set; }
    void Initialize();
}
