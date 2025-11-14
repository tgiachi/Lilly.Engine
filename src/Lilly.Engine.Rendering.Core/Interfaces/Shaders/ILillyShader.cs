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

    /// <summary>
    /// Configures and enables a vertex attribute.
    /// </summary>
    /// <param name="name">The name of the vertex attribute.</param>
    /// <param name="size">The number of components per vertex attribute (1, 2, 3, or 4).</param>
    /// <param name="type">The data type of each component.</param>
    /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
    /// <param name="offset">The offset of the first component of the first vertex attribute.</param>
    /// <param name="normalized">Whether fixed-point data values should be normalized.</param>
    void SetVertexAttrib(string name, int size, Silk.NET.OpenGL.VertexAttribPointerType type,
                        uint stride, int offset, bool normalized = false);

    /// <summary>
    /// Disables a vertex attribute array.
    /// </summary>
    /// <param name="name">The name of the vertex attribute to disable.</param>
    void DisableVertexAttrib(string name);

    /// <summary>
    /// Outputs debug information about all active vertex attributes in this shader program.
    /// Useful for troubleshooting and verifying shader state.
    /// </summary>
    void DebugAttributes();



    /// <summary>
    /// Configures a single float vertex attribute with automatic type detection.
    /// </summary>
    /// <param name="name">The name of the vertex attribute.</param>
    /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
    /// <param name="offset">The offset of the first component.</param>
    void SetVertexAttribFloat(string name, uint stride, int offset);

    /// <summary>
    /// Configures a Vector2D&lt;float&gt; vertex attribute with automatic type detection.
    /// </summary>
    /// <param name="name">The name of the vertex attribute.</param>
    /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
    /// <param name="offset">The offset of the first component.</param>
    void SetVertexAttribVector2(string name, uint stride, int offset);

    /// <summary>
    /// Configures a Vector3D&lt;float&gt; vertex attribute with automatic type detection.
    /// </summary>
    /// <param name="name">The name of the vertex attribute.</param>
    /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
    /// <param name="offset">The offset of the first component.</param>
    void SetVertexAttribVector3(string name, uint stride, int offset);

    /// <summary>
    /// Configures a Vector4D&lt;float&gt; vertex attribute with automatic type detection.
    /// </summary>
    /// <param name="name">The name of the vertex attribute.</param>
    /// <param name="stride">The byte offset between consecutive vertex attributes.</param>
    /// <param name="offset">The offset of the first component.</param>
    void SetVertexAttribVector4(string name, uint stride, int offset);
}
