namespace Lilly.Engine.Exceptions;

/// <summary>
/// Exception thrown when a requested uniform variable is not found in the shader program.
/// </summary>
public class ShaderUniformNotFoundException : Exception
{
    /// <summary>
    /// Gets the name of the uniform that was not found.
    /// </summary>
    public string UniformName { get; }

    /// <summary>
    /// Gets the OpenGL program handle where the uniform was searched.
    /// </summary>
    public uint ProgramHandle { get; }

    /// <summary>
    /// Gets the list of available uniforms in the shader program.
    /// </summary>
    public IReadOnlyList<string> AvailableUniforms { get; }

    public ShaderUniformNotFoundException(
        string uniformName,
        uint programHandle,
        IReadOnlyList<string> availableUniforms = null,
        Exception innerException = null
    )
        : base($"Uniform '{uniformName}' not found in shader program {programHandle}", innerException)
    {
        UniformName = uniformName;
        ProgramHandle = programHandle;
        AvailableUniforms = availableUniforms ?? Array.Empty<string>();
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        var availableUniformsStr = AvailableUniforms.Count > 0
            ? string.Join(", ", AvailableUniforms)
            : "No uniforms available";

        return $"{baseString}\n\nProgram Handle: {ProgramHandle}\n" +
               $"Requested Uniform: {UniformName}\n" +
               $"Available Uniforms: [{availableUniformsStr}]";
    }
}
