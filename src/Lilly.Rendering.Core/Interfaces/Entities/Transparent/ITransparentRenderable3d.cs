using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Rendering.Core.Interfaces.Camera;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.Entities.Transparent;

/// <summary>
/// Adds an explicit transparent rendering pass for 3D game objects.
/// </summary>
public interface ITransparentRenderable3d
{
    void DrawTransparent(GameTime gameTime, GraphicsDevice graphicsDevice, ICamera3D camera);
}
