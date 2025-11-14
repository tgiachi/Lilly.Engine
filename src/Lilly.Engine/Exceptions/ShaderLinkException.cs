namespace Lilly.Engine.Exceptions;

/// <summary>
/// Exception thrown when shader program linking fails.
/// </summary>
public class ShaderLinkException : Exception
{
    /// <summary>
    /// Gets the OpenGL program handle that failed to link.
    /// </summary>
    public uint ProgramHandle { get; }

    /// <summary>
    /// Gets the linking info log from OpenGL.
    /// </summary>
    public string InfoLog { get; }

    public ShaderLinkException(
        uint programHandle,
        string infoLog,
        Exception innerException = null
    )
        : base($"Program failed to link: {infoLog}", innerException)
    {
        ProgramHandle = programHandle;
        InfoLog = infoLog;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}\n\nProgram Handle: {ProgramHandle}\n" +
               $"Info Log:\n{InfoLog}";
    }
}
