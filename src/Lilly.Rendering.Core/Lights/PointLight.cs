using System.Numerics;
using Lilly.Rendering.Core.Interfaces.Lights;
using Lilly.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Rendering.Core.Lights;

/// <summary>
/// Point light that radiates in all directions (e.g., lightbulb, torch, candle).
/// Intensity decreases with distance based on attenuation parameters.
/// </summary>
public class PointLight : ILight
{
    /// <summary>
    /// Identifier name of the light
    /// </summary>
    public string Name { get; set; } = "PointLight";

    /// <summary>
    /// Type of light (always Point for this class)
    /// </summary>
    public LightType LightType => LightType.Point;

    /// <summary>
    /// RGB color of the light (0-1 per channel)
    /// </summary>
    public Color4b Color { get; set; } = Color4b.White;

    /// <summary>
    /// Whether the light is active and should be rendered
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether this light casts shadows
    /// </summary>
    public bool CastsShadows { get; set; }

    /// <summary>
    /// Intensity/brightness (multiplier of color)
    /// </summary>
    public float Intensity { get; set; } = 1.0f;

    /// <summary>
    /// Position of the light in 3D world space
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Maximum radius of influence of this light
    /// </summary>
    public float Radius { get; set; } = 10.0f;

    /// <summary>
    /// Constant term of attenuation (default: 1.0)
    /// Formula: Attenuation = 1.0 / (Constant + Linear * d + Quadratic * d²)
    /// </summary>
    public float Constant { get; set; } = 1.0f;

    /// <summary>
    /// Linear term of attenuation (default: 0.09)
    /// Controls the speed of light decay
    /// </summary>
    public float Linear { get; set; } = 0.09f;

    /// <summary>
    /// Quadratic term of attenuation (default: 0.032)
    /// Controls the acceleration of light decay
    /// </summary>
    public float Quadratic { get; set; } = 0.032f;

    /// <summary>
    /// Calculates the attenuation value at a given distance
    /// </summary>
    /// <param name="distance">Distance from the light</param>
    /// <returns>Attenuation value between 0.0 (no light) and 1.0 (maximum intensity)</returns>
    public float CalculateAttenuation(float distance)
    {
        if (distance > Radius)
        {
            return 0.0f;
        }

        return 1.0f / (Constant + Linear * distance + Quadratic * distance * distance);
    }

    /// <summary>
    /// Sets attenuation parameters automatically based on desired radius.
    /// Uses pre-calculated optimal values for common ranges.
    /// </summary>
    /// <param name="radius">Desired radius of influence</param>
    public void SetAttenuationPreset(float radius)
    {
        Radius = radius;

        // Preset ottimizzati per diversi range
        switch (radius)
        {
            case <= 7:
                Constant = 1.0f;
                Linear = 0.7f;
                Quadratic = 1.8f;

                break;

            case <= 13:
                Constant = 1.0f;
                Linear = 0.35f;
                Quadratic = 0.44f;

                break;

            case <= 20:
                Constant = 1.0f;
                Linear = 0.22f;
                Quadratic = 0.20f;

                break;

            case <= 32:
                Constant = 1.0f;
                Linear = 0.14f;
                Quadratic = 0.07f;

                break;

            case <= 50:
                Constant = 1.0f;
                Linear = 0.09f;
                Quadratic = 0.032f;

                break;

            case <= 65:
                Constant = 1.0f;
                Linear = 0.07f;
                Quadratic = 0.017f;

                break;

            case <= 100:
                Constant = 1.0f;
                Linear = 0.045f;
                Quadratic = 0.0075f;

                break;

            case <= 160:
                Constant = 1.0f;
                Linear = 0.027f;
                Quadratic = 0.0028f;

                break;

            case <= 200:
                Constant = 1.0f;
                Linear = 0.022f;
                Quadratic = 0.0019f;

                break;

            case <= 325:
                Constant = 1.0f;
                Linear = 0.014f;
                Quadratic = 0.0007f;

                break;

            case <= 600:
                Constant = 1.0f;
                Linear = 0.007f;
                Quadratic = 0.0002f;

                break;

            default: // > 600
                Constant = 1.0f;
                Linear = 0.0014f;
                Quadratic = 0.000007f;

                break;
        }
    }

    /// <summary>
    /// Gets the effective color of the light (Color * Intensity)
    /// </summary>
    /// <returns>Effective color in Color4b format</returns>
    public Color4b GetEffectiveColor()
    {
        var effectiveColor = Color * Intensity;

        return new(
            (byte)Math.Min(effectiveColor.R * 255f, 255),
            (byte)Math.Min(effectiveColor.G * 255f, 255),
            (byte)Math.Min(effectiveColor.B * 255f, 255)
        );
    }

    /// <summary>
    /// Checks if a point is within the radius of influence of this light
    /// </summary>
    /// <param name="point">The point to check</param>
    /// <returns>True if the point is affected by this light</returns>
    public bool IsPointInRange(Vector3 point)
    {
        return Vector3.Distance(Position, point) <= Radius;
    }

    /// <summary>
    /// Calculates the distance from this light to a point
    /// </summary>
    /// <param name="point">The target point</param>
    /// <returns>Distance in world units</returns>
    public float GetDistanceToPoint(Vector3 point)
    {
        return Vector3.Distance(Position, point);
    }

    /// <summary>
    /// Calculates the normalized direction from this light to a point
    /// </summary>
    /// <param name="point">The target point</param>
    /// <returns>Normalized direction vector</returns>
    public Vector3 GetDirectionToPoint(Vector3 point)
    {
        return Vector3.Normalize(point - Position);
    }

    /// <summary>
    /// Applies this light's parameters to the specified shader
    /// </summary>
    /// <param name="shader">The shader to apply parameters to</param>
    /// <param name="uniformPrefix">Uniform prefix (e.g., "uPointLights[0]")</param>
    public void ApplyToShader(ShaderProgram shader, string uniformPrefix)
    {
        var positionUniform = shader.Uniforms[$"{uniformPrefix}.position"];
        var colorUniform = shader.Uniforms[$"{uniformPrefix}.color"];
        var radiusUniform = shader.Uniforms[$"{uniformPrefix}.radius"];
        var constantUniform = shader.Uniforms[$"{uniformPrefix}.constant"];
        var linearUniform = shader.Uniforms[$"{uniformPrefix}.linear"];
        var quadraticUniform = shader.Uniforms[$"{uniformPrefix}.quadratic"];

        if (positionUniform.IsEmpty ||
            colorUniform.IsEmpty ||
            radiusUniform.IsEmpty ||
            constantUniform.IsEmpty ||
            linearUniform.IsEmpty ||
            quadraticUniform.IsEmpty)
        {
            return;
        }

        positionUniform.SetValueVec3(Position);
        colorUniform.SetValueVec4(GetEffectiveColor());
        radiusUniform.SetValueFloat(Radius);
        constantUniform.SetValueFloat(Constant);
        linearUniform.SetValueFloat(Linear);
        quadraticUniform.SetValueFloat(Quadratic);
    }
}
