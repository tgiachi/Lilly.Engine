using System.Numerics;
using Lilly.Rendering.Core.Interfaces.Entities;
using Lilly.Rendering.Core.Interfaces.Services;
using TrippyGL;

namespace Lilly.Engine.GameObjects.Base;

/// <summary>
/// Base 3D game object that can render into the shadow map.
/// </summary>
public abstract class Base3dShadowGameObject : Base3dGameObject, IShadowCaster3d, IShadowReceiver3d
{
    public virtual bool ReceiveShadows { get; set; } = true;

    protected Base3dShadowGameObject(string name, IGameObjectManager gameObjectManager, uint zIndex = 0)
        : base(name, gameObjectManager, zIndex)
    {
    }

    public void DrawShadow(ShaderProgram shadowShader, Matrix4x4 lightView, Matrix4x4 lightProjection)
    {
        if (!IsActive)
        {
            return;
        }

        SetShadowUniforms(shadowShader, lightView, lightProjection);
        DrawShadowGeometry(shadowShader);
    }

    /// <summary>
    /// Sets common shadow uniforms; override to customize.
    /// </summary>
    protected virtual void SetShadowUniforms(ShaderProgram shadowShader, Matrix4x4 lightView, Matrix4x4 lightProjection)
    {
        shadowShader.Uniforms["uWorld"].SetValueMat4(Transform.GetTransformationMatrix());
        shadowShader.Uniforms["uLightView"].SetValueMat4(lightView);
        shadowShader.Uniforms["uLightProjection"].SetValueMat4(lightProjection);
    }

    /// <summary>
    /// Draws the geometry into the bound shadow framebuffer.
    /// Derived classes must implement the actual draw calls.
    /// </summary>
    protected abstract void DrawShadowGeometry(ShaderProgram shadowShader);
}
