using Silk.NET.Maths;

namespace Lilly.Engine.Rendering.Core.Interfaces.Shaders;

public interface ILillyShader
{
    /// <summary>
    /// Gets the OpenGL handle for this shader program.
    /// </summary>
    uint Handle { get; }

    /// <summary>
    ///  Compiles and links the shader program from the provided source code.
    /// </summary>
    /// <param name="source"></param>
    void CompileAndLink(string source);

    /// <summary>
    /// Activates this shader program for subsequent rendering operations.
    /// </summary>
    void Use();

    /// <summary>
    /// Retrieves the index of a named uniform block within this shader program.
    /// </summary>
    /// <param name="name">The name of the uniform block.</param>
    /// <returns>The index of the uniform block.</returns>
    uint GetUniformBlockIndex(string name);

    /// <summary>
    /// Retrieves the location of a named uniform variable in this shader program.
    /// Performs lazy caching if the uniform was not cached during initialization.
    /// </summary>
    /// <param name="name">The name of the uniform variable.</param>
    /// <returns>The location of the uniform variable.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the uniform is not found.</exception>
    int GetUniformLocation(string name);

    /// <summary>
    /// Retrieves the location of a named vertex attribute in this shader program.
    /// </summary>
    /// <param name="attribName">The name of the vertex attribute.</param>
    /// <returns>The location of the vertex attribute, or -1 if not found.</returns>
    int GetAttribLocation(string attribName);

    /// <summary>
    /// Sets an integer uniform value.
    /// Automatically activates the shader program and performs error checking.
    /// </summary>
    /// <param name="name">The name of the uniform variable.</param>
    /// <param name="value">The integer value to set.</param>
    void SetUniform(string name, int value);

    /// <summary>
    /// Sets a floating-point uniform value.
    /// Automatically activates the shader program and performs error checking.
    /// </summary>
    /// <param name="name">The name of the uniform variable.</param>
    /// <param name="value">The float value to set.</param>
    void SetUniform(string name, float value);

    /// <summary>
    /// Sets a 3-component vector uniform value.
    /// Automatically activates the shader program and performs error checking.
    /// </summary>
    /// <param name="name">The name of the uniform variable.</param>
    /// <param name="value">The Vector3 value to set.</param>
    void SetUniform(string name, Vector3D<float> value);

    /// <summary>
    /// Sets a 4-component vector uniform value.
    /// Automatically activates the shader program and performs error checking.
    /// </summary>
    /// <param name="name">The name of the uniform variable.</param>
    /// <param name="value">The Vector4 value to set.</param>
    void SetUniform(string name, Vector4D<float> value);

    /// <summary>
    /// Sets a 4x4 matrix uniform value.
    /// Automatically activates the shader program and performs error checking.
    /// </summary>
    /// <param name="name">The name of the uniform variable.</param>
    /// <param name="value">The Matrix4x4 value to set.</param>
    void SetUniform(string name, Matrix4X4<float> value);

    /// <summary>
    /// Outputs debug information about all active uniforms in this shader program.
    /// Useful for troubleshooting and verifying shader state.
    /// </summary>
    void DebugUniforms();
}
