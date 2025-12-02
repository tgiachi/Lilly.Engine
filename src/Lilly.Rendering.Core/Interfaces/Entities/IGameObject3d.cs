using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Primitives;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.Entities;

public interface IGameObject3d : IGameObject
{

    bool IgnoreFrustumCulling { get; set; }
    Transform3D Transform { get; set; }
    BoundingBox BoundingBox { get; }

    void Draw(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera);

}
