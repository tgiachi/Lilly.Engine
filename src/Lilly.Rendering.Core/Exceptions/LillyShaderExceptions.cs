using Silk.NET.OpenGL;

namespace Lilly.Rendering.Core.Exceptions;

public sealed class ShaderCompilationException : Exception
{
    public ShaderType ShaderType { get; }
    public string Path { get; }

    public ShaderCompilationException(ShaderType type, string path, string log)
        : base($"Shader compilation failed for {path} ({type}): {log}")
    {
        ShaderType = type;
        Path = path;
    }
}

public sealed class ShaderLinkException : Exception
{
    public string VertexPath { get; }
    public string FragmentPath { get; }

    public ShaderLinkException(string vertexPath, string fragmentPath, string log)
        : base($"Shader link failed for program [{vertexPath}, {fragmentPath}]: {log}")
    {
        VertexPath = vertexPath;
        FragmentPath = fragmentPath;
    }
}

public sealed class UniformNotFoundException : Exception
{
    public UniformNotFoundException(string name)
        : base($"Uniform '{name}' not found on shader.") { }
}
