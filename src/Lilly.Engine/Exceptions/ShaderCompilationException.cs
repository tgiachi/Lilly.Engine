using Silk.NET.OpenGL;

namespace Lilly.Engine.Exceptions;

/// <summary>
/// Exception thrown when shader compilation fails.
/// </summary>
public class ShaderCompilationException : Exception
{
    /// <summary>
    /// Gets the type of shader that failed to compile (Vertex, Fragment, etc.).
    /// </summary>
    public ShaderType ShaderType { get; }

    /// <summary>
    /// Gets the shader source code that failed to compile.
    /// </summary>
    public string ShaderSource { get; }

    /// <summary>
    /// Gets the compilation info log from OpenGL.
    /// </summary>
    public string InfoLog { get; }

    public ShaderCompilationException(
        ShaderType shaderType,
        string shaderSource,
        string infoLog,
        Exception innerException = null
    )
        : base($"Error compiling shader of type {shaderType}: {infoLog}", innerException)
    {
        ShaderType = shaderType;
        ShaderSource = shaderSource;
        InfoLog = infoLog;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        return $"{baseString}\n\nShader Type: {ShaderType}\n" +
               $"Info Log:\n{InfoLog}\n" +
               $"Source:\n{ShaderSource}";
    }
}
