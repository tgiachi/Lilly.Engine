using System.Numerics;
using Lilly.Rendering.Core.Types;
using Silk.NET.OpenGL;
using TrippyGL;

namespace Lilly.Rendering.Core.Interfaces.Lights;

/// <summary>
/// Base interface for all light sources in the engine
/// </summary>
public interface ILight
{
    /// <summary>
    /// Identifier name of the light
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Type of light (Directional, Point, Spot)
    /// </summary>
    LightType LightType { get; }

    /// <summary>
    /// Whether the light is active and should be rendered
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Whether this light casts shadows
    /// </summary>
    bool CastsShadows { get; set; }

    /// <summary>
    /// Intensity/brightness of the light (multiplier)
    /// </summary>
    float Intensity { get; set; }

    /// <summary>
    /// Gets the effective color of the light (Color * Intensity)
    /// </summary>
    /// <returns>Effective color in Color4b format</returns>
    Color4b GetEffectiveColor();

    /// <summary>
    /// Applies this light's parameters to the specified shader
    /// </summary>
    /// <param name="shader">The shader to apply parameters to</param>
    /// <param name="uniformPrefix">Uniform prefix (e.g., "uPointLights[0]")</param>
    void ApplyToShader(ShaderProgram shader, string uniformPrefix);
}
