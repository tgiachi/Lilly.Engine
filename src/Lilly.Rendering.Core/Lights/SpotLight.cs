using System.Numerics;
using Lilly.Rendering.Core.Interfaces.Lights;
using Lilly.Rendering.Core.Types;
using TrippyGL;

namespace Lilly.Rendering.Core.Lights;

/// <summary>
/// Spot light in a cone shape (e.g., reflector, spotlight).
/// Has position, direction, and cone angle parameters.
/// </summary>
public class SpotLight : ILight
{
    /// <summary>
    /// Identifier name of the light
    /// </summary>
    public string Name { get; set; } = "SpotLight";

    /// <summary>
    /// RGB color of the light (0-1 per channel)
    /// </summary>
    public Color4b Color { get; set; } = Color4b.White;

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
    /// Type of light (always Spot for this class)
    /// </summary>
    public LightType LightType => LightType.Spot;

    /// <summary>
    /// Position of the light in 3D world space
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// Direction the light cone is pointing (normalized automatically)
    /// </summary>
    public Vector3 Direction
    {
        get => _direction;
        set => _direction = Vector3.Normalize(value);
    }

    private Vector3 _direction = new(0, -1, 0);

    /// <summary>
    /// Inner cone angle in radians (area with full intensity)
    /// Default: π/6 (30 degrees)
    /// </summary>
    public float InnerConeAngle { get; set; } = MathF.PI / 6f;

    /// <summary>
    /// Outer cone angle in radians (area with smooth transition to darkness)
    /// Default: π/4 (45 degrees)
    /// </summary>
    public float OuterConeAngle { get; set; } = MathF.PI / 4f;

    /// <summary>
    /// Maximum distance of the light cone
    /// </summary>
    public float Range { get; set; } = 20.0f;

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
    /// Applies this light's parameters to the specified shader
    /// </summary>
    /// <param name="shader">The shader to apply parameters to</param>
    /// <param name="uniformPrefix">Uniform prefix (e.g., "uSpotLights[0]")</param>
    public void ApplyToShader(ShaderProgram shader, string uniformPrefix)
    {
        var positionUniform = shader.Uniforms[$"{uniformPrefix}.position"];
        var directionUniform = shader.Uniforms[$"{uniformPrefix}.direction"];
        var colorUniform = shader.Uniforms[$"{uniformPrefix}.color"];
        var innerCutoffUniform = shader.Uniforms[$"{uniformPrefix}.innerCutoff"];
        var outerCutoffUniform = shader.Uniforms[$"{uniformPrefix}.outerCutoff"];
        var rangeUniform = shader.Uniforms[$"{uniformPrefix}.range"];
        var constantUniform = shader.Uniforms[$"{uniformPrefix}.constant"];
        var linearUniform = shader.Uniforms[$"{uniformPrefix}.linear"];
        var quadraticUniform = shader.Uniforms[$"{uniformPrefix}.quadratic"];

        if (positionUniform.IsEmpty ||
            directionUniform.IsEmpty ||
            colorUniform.IsEmpty ||
            innerCutoffUniform.IsEmpty ||
            outerCutoffUniform.IsEmpty ||
            rangeUniform.IsEmpty ||
            constantUniform.IsEmpty ||
            linearUniform.IsEmpty ||
            quadraticUniform.IsEmpty)
        {
            return;
        }

        positionUniform.SetValueVec3(Position);
        directionUniform.SetValueVec3(Direction);
        colorUniform.SetValueVec4(GetEffectiveColor());
        innerCutoffUniform.SetValueFloat(MathF.Cos(InnerConeAngle));
        outerCutoffUniform.SetValueFloat(MathF.Cos(OuterConeAngle));
        rangeUniform.SetValueFloat(Range);
        constantUniform.SetValueFloat(Constant);
        linearUniform.SetValueFloat(Linear);
        quadraticUniform.SetValueFloat(Quadratic);
    }

    /// <summary>
    /// Gets the effective color of the light (Color * Intensity)
    /// </summary>
    /// <returns>Effective color in Color4b format</returns>
    public Color4b GetEffectiveColor()
    {
        var effectiveColor = Color * Intensity;
        var vec3EffectiveColor = Vector3.Clamp(effectiveColor.ToVector3(), Vector3.Zero, Vector3.One);

        return new Color4b(
            (byte)(vec3EffectiveColor.X * 255f),
            (byte)(vec3EffectiveColor.Y * 255f),
            (byte)(vec3EffectiveColor.Z * 255f)
        );
    }

    /// <summary>
    /// Checks if a point is inside the cone of light
    /// </summary>
    /// <param name="point">The point to check</param>
    /// <returns>True if the point is illuminated by this spot light</returns>
    public bool IsPointInCone(Vector3 point)
    {
        var lightToPoint = point - Position;
        var distance = lightToPoint.Length();

        if (distance > Range)
        {
            return false;
        }

        var lightDirection = Vector3.Normalize(lightToPoint);
        var angle = MathF.Acos(Vector3.Dot(lightDirection, Direction));

        return angle <= OuterConeAngle;
    }

    /// <summary>
    /// Calculates the total light intensity at a specific point
    /// </summary>
    /// <param name="point">The target point</param>
    /// <returns>Intensity value between 0 (unlit) and 1 (maximum intensity)</returns>
    public float CalculateIntensityAtPoint(Vector3 point)
    {
        var lightToPoint = point - Position;
        var distance = lightToPoint.Length();

        if (distance > Range)
            return 0.0f;

        var lightDirection = Vector3.Normalize(lightToPoint);
        var cosTheta = Vector3.Dot(lightDirection, Direction);

        var cosOuterCutoff = MathF.Cos(OuterConeAngle);

        if (cosTheta < cosOuterCutoff)
            return 0.0f;

        var attenuation = 1.0f / (Constant + Linear * distance + Quadratic * distance * distance);

        var cosInnerCutoff = MathF.Cos(InnerConeAngle);
        var epsilon = cosInnerCutoff - cosOuterCutoff;
        var spotIntensity = MathF.Max(0.0f, (cosTheta - cosOuterCutoff) / epsilon);
        spotIntensity = MathF.Min(1.0f, spotIntensity);

        return attenuation * spotIntensity;
    }

    /// <summary>
    /// Calculates the attenuation value based on distance (ignores cone angle)
    /// </summary>
    /// <param name="distance">Distance from the light</param>
    /// <returns>Attenuation value between 0.0 (no light) and 1.0 (maximum intensity)</returns>
    public float CalculateAttenuation(float distance)
    {
        if (distance > Range)
            return 0.0f;

        return 1.0f / (Constant + Linear * distance + Quadratic * distance * distance);
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
    /// Sets attenuation parameters automatically based on desired range.
    /// Uses pre-calculated optimal values for common ranges.
    /// </summary>
    /// <param name="range">Desired range of influence</param>
    public void SetAttenuationPreset(float range)
    {
        Range = range;

        switch (range)
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

            default:
                Constant = 1.0f;
                Linear = 0.0014f;
                Quadratic = 0.000007f;

                break;
        }
    }

    /// <summary>
    /// Points the spotlight to look at a target position
    /// </summary>
    /// <param name="target">The position to look at</param>
    public void LookAt(Vector3 target)
    {
        Direction = Vector3.Normalize(target - Position);
    }

    /// <summary>
    /// Sets the cone angles in degrees (converted to radians internally)
    /// </summary>
    /// <param name="innerDegrees">Inner cone angle in degrees</param>
    /// <param name="outerDegrees">Outer cone angle in degrees</param>
    public void SetConeAnglesDegrees(float innerDegrees, float outerDegrees)
    {
        InnerConeAngle = innerDegrees * MathF.PI / 180f;
        OuterConeAngle = outerDegrees * MathF.PI / 180f;
    }

    /// <summary>
    /// Gets the inner cone angle in degrees
    /// </summary>
    /// <returns>Inner cone angle in degrees</returns>
    public float GetInnerConeAngleDegrees()
    {
        return InnerConeAngle * 180f / MathF.PI;
    }

    /// <summary>
    /// Gets the outer cone angle in degrees
    /// </summary>
    /// <returns>Outer cone angle in degrees</returns>
    public float GetOuterConeAngleDegrees()
    {
        return OuterConeAngle * 180f / MathF.PI;
    }
}
