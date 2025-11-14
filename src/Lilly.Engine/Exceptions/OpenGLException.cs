using Silk.NET.OpenGL;

namespace Lilly.Engine.Exceptions;

/// <summary>
/// Exception thrown when an OpenGL error occurs.
/// </summary>
public class OpenGLException : Exception
{
    /// <summary>
    /// Gets the OpenGL error code.
    /// </summary>
    public GLEnum ErrorCode { get; }

    /// <summary>
    /// Gets the context in which the error occurred (e.g., "SetUniform", "Use", etc.).
    /// </summary>
    public string Context { get; }

    public OpenGLException(
        GLEnum errorCode,
        string context = null,
        Exception innerException = null
    )
        : base(BuildMessage(errorCode, context), innerException)
    {
        ErrorCode = errorCode;
        Context = context ?? "Unknown";
    }

    private static string BuildMessage(GLEnum errorCode, string context)
    {
        var message = $"OpenGL Error: {errorCode}";
        if (!string.IsNullOrWhiteSpace(context))
        {
            message += $" (Context: {context})";
        }
        return message;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}\n\nError Code: {ErrorCode}\n" +
               $"Context: {Context}";
    }
}
