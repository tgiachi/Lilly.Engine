namespace Lilly.Engine.Exceptions;

/// <summary>
/// Exception thrown when shader source code parsing fails.
/// </summary>
public class ShaderSourceParseException : Exception
{
    /// <summary>
    /// Gets the shader source code that failed to parse.
    /// </summary>
    public string ShaderSource { get; }

    /// <summary>
    /// Gets the reason why parsing failed.
    /// </summary>
    public string Reason { get; }

    public ShaderSourceParseException(
        string shaderSource,
        string reason,
        Exception innerException = null
    )
        : base($"Failed to parse shader source: {reason}", innerException)
    {
        ShaderSource = shaderSource;
        Reason = reason;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        var sourcePreview = ShaderSource.Length > 200
                                ? string.Concat(ShaderSource.AsSpan(0, 200), "...")
                                : ShaderSource;

        return $"{baseString}\n\nReason: {Reason}\n" +
               $"Source Preview:\n{sourcePreview}";
    }
}
