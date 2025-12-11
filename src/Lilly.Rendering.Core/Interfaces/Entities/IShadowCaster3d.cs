using System.Numerics;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.Entities;

/// <summary>
/// Optional contract for objects that can render into the shadow map.
/// </summary>
public interface IShadowCaster3d
{
    void DrawShadow(ShaderProgram shadowShader, Matrix4x4 lightView, Matrix4x4 lightProjection);
}
