using Lilly.Rendering.Core.Interfaces.Camera;
using Lilly.Rendering.Core.Interfaces.Entities;
using TrippyGL;

namespace Lilly.Engine.Extensions;

public static class ShadersExtensions
{
    extension(SimpleShaderProgram shaderProgram)
    {
        public void UpdateView(IGameObject3d gameObject3d, ICamera3D camera)
        {
            shaderProgram.Projection = camera.Projection;
            shaderProgram.View = camera.View;
            shaderProgram.World = gameObject3d.Transform.GetTransformationMatrix();
        }
    }

}
