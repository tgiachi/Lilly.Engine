using System.Numerics;
using Lilly.Rendering.Core.Interfaces.Lights;
using Lilly.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Rendering.Core.Lights;

/// <summary>
/// Directional light with parallel rays (e.g., sun, moon).
/// Does not have position, only direction.
/// </summary>
public class DirectionalLight : ILight
{
    /// <summary>
    /// Identifier name of the light
    /// </summary>
    public string Name { get; set; } = "DirectionalLight";

    /// <summary>
    /// RGB color of the light (0-1 per channel)
    /// </summary>
    public Vector3 Color { get; set; } = Vector3.One;

    /// <summary>
    /// Intensity/brightness (multiplier of color)
    /// </summary>
    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Whether the light is active and should be rendered
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this light casts shadows
    /// </summary>
    public bool CastsShadows { get; set; }

    /// <summary>
    /// Type of light (always Directional for this class)
    /// </summary>
    public LightType LightType => LightType.Directional;

    /// <summary>
    /// Direction the light is pointing (normalized automatically)
    /// </summary>
    public Vector3 Direction { get; set; } = Vector3.Normalize(new(-0.4f, -1.0f, -0.2f));

    /// <summary>
    /// Gets the effective color of the light (Color * Intensity)
    /// </summary>
    /// <returns>Effective color in Color4b format</returns>
    public Color4b GetEffectiveColor()
    {
        return new Color4b(Color * Intensity);
    }

    /// <summary>
    /// Applies this light's parameters to the specified shader
    /// </summary>
    /// <param name="shader">The shader to apply parameters to</param>
    /// <param name="uniformPrefix">Uniform prefix (e.g., "uDirectionalLights[0]")</param>
    public void ApplyToShader(ShaderProgram shader, string uniformPrefix)
    {
        if (!IsActive)
        {
            return;
        }

        shader.Uniforms[$"{uniformPrefix}.direction"].SetValueVec3(Direction);
        shader.Uniforms[$"{uniformPrefix}.color"].SetValueVec4(GetEffectiveColor());
        shader.Uniforms[$"{uniformPrefix}.castShadows"].SetValueBool(CastsShadows);
    }

    /// <summary>
    /// Calculates the view matrix for shadow mapping from this light
    /// </summary>
    /// <param name="targetCenter">Center of the area to shadow</param>
    /// <param name="distance">Distance from target to light position</param>
    /// <returns>View matrix for shadow mapping</returns>
    public Matrix4x4 GetShadowViewMatrix(Vector3 targetCenter, float distance = 100f)
    {
        var lightPos = targetCenter - Direction * distance;
        return Matrix4x4.CreateLookAt(lightPos, targetCenter, Vector3.UnitY);
    }
}
