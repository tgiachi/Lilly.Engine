using TrippyGL;

namespace Lilly.Engine.Extensions;

/// <summary>
/// Provides extension methods for shader program uniform handling.
/// </summary>
public static class ShaderProgramExtensions
{
    /// <summary>
    /// Safely sets a shader uniform if it exists and is not empty.
    /// </summary>
    /// <param name="shader">The shader program.</param>
    /// <param name="uniformName">The name of the uniform.</param>
    /// <param name="setter">An action to set the uniform value.</param>
    public static void TrySetUniform(this ShaderProgram shader, string uniformName, Action<ShaderUniform> setter)
    {
        var uniform = shader.Uniforms[uniformName];

        if (!uniform.IsEmpty)
        {
            setter(uniform);
        }
    }
}
