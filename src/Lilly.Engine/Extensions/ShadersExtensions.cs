using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using TrippyGL;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Provides extension methods for updating shader programs with camera and game object transformations.
/// </summary>
/// <summary>
/// Provides extension methods for updating shader programs with camera and transformation data.
/// </summary>
public static class ShadersExtensions
{
    /// <summary>
    /// Updates the shader program with the view, projection, and world matrices from the camera and game object.
    /// </summary>
    /// <param name="gameObject3d">The 3D game object to get the world matrix from.</param>
    /// <param name="camera">The camera to get the view and projection matrices from.</param>
    public static void UpdateView(this SimpleShaderProgram shaderProgram, IGameObject3d gameObject3d, ICamera3D camera)
    {
        shaderProgram.Projection = camera.Projection;
        shaderProgram.View = camera.View;
        shaderProgram.World = gameObject3d.Transform.GetTransformationMatrix();
    }
}
