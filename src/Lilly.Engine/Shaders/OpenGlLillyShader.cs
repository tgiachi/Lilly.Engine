using Lilly.Engine.Exceptions;
using Lilly.Engine.Rendering.Core.Interfaces.Camera;
using Lilly.Engine.Rendering.Core.Interfaces.Shaders;
using Serilog;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Lilly.Engine.Shaders;

public class OpenGlLillyShader : ILillyShader, IDisposable
{
    private readonly GL _gl;
    private readonly ILogger _logger = Log.ForContext<OpenGlLillyShader>();
    private readonly Dictionary<string, int> _uniformLocations = new();
    private bool _disposed;

    public uint Handle { get; private set; }

    public OpenGlLillyShader(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
    }

    public void CompileAndLink(string source)
    {
        // Clear any previous OpenGL errors
        while (_gl.GetError() != GLEnum.NoError) { }

        // Parse the source to extract vertex and fragment shaders
        // Expected format: source contains both shaders separated by a delimiter
        // For example: "#shader vertex\n...\n#shader fragment\n..."

        var shaders = ParseShaderSource(source);

        if (!shaders.TryGetValue(ShaderType.VertexShader, out string? value) ||
            !shaders.TryGetValue(ShaderType.FragmentShader, out string? value1))
        {
            throw new ShaderSourceParseException(
                source,
                "Source must contain both vertex and fragment shaders. " +
                "Expected format: '#shader vertex' followed by vertex shader code, " +
                "then '#shader fragment' followed by fragment shader code."
            );
        }

        uint vertex = CompileShader(ShaderType.VertexShader, value);
        uint fragment = CompileShader(ShaderType.FragmentShader, value1);

        Handle = _gl.CreateProgram();
        _gl.AttachShader(Handle, vertex);
        _gl.AttachShader(Handle, fragment);
        _gl.LinkProgram(Handle);

        _gl.GetProgram(Handle, GLEnum.LinkStatus, out var status);

        if (status == 0)
        {
            throw new ShaderLinkException(Handle, _gl.GetProgramInfoLog(Handle));
        }

        _gl.DetachShader(Handle, vertex);
        _gl.DetachShader(Handle, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        CheckGLError("CompileAndLink");

        // Cache all uniform locations
        CacheUniforms();
    }

    public void Use()
    {
        _gl.UseProgram(Handle);
    }

    public uint GetUniformBlockIndex(string name)
    {
        return _gl.GetUniformBlockIndex(Handle, name);
    }

    public int GetUniformLocation(string name)
    {
        if (!_uniformLocations.TryGetValue(name, out int value))
        {
            int location = _gl.GetUniformLocation(Handle, name);

            if (location == -1)
            {
                var availableUniforms = _uniformLocations.Keys.ToList();

                throw new ShaderUniformNotFoundException(name, Handle, availableUniforms);
            }

            value = location;
            _uniformLocations.Add(name, value);
        }

        return value;
    }

    public int GetAttribLocation(string attribName)
    {
        var result = _gl.GetAttribLocation(Handle, attribName);
        CheckGLError($"GetAttribLocation({attribName})");

        return result;
    }

    public void SetUniform(string name, int value)
    {
        int location = GetUniformLocation(name);
        _gl.UseProgram(Handle);
        _gl.Uniform1(location, value);
        CheckGLError($"SetUniform({name}, int)");
    }

    public void SetUniform(string name, float value)
    {
        int location = GetUniformLocation(name);
        _gl.Uniform1(location, value);
        CheckGLError($"SetUniform({name}, float)");
    }

    public void SetUniform(string name, Vector3D<float> value)
    {
        int location = GetUniformLocation(name);
        _gl.Uniform3(location, value.X, value.Y, value.Z);
        CheckGLError($"SetUniform({name}, Vector3D)");
    }

    public void SetUniform(string name, Vector4D<float> value)
    {
        int location = GetUniformLocation(name);
        _gl.Uniform4(location, value.X, value.Y, value.Z, value.W);
        CheckGLError($"SetUniform({name}, Vector4D)");
    }

    public unsafe void SetUniform(string name, Matrix4X4<float> value)
    {
        int location = GetUniformLocation(name);
        _gl.UniformMatrix4(location, 1, false, (float*)&value);
        CheckGLError($"SetUniform({name}, Matrix4X4)");
    }

    /// <summary>
    /// Sets the camera matrices (uView and uProjection) in the shader.
    /// Also sets camera-related vectors (uCameraPosition, uCameraForward, uCameraRight, uCameraUp) if they exist.
    /// </summary>
    /// <param name="camera">The camera to use</param>
    /// <param name="setWorld">If true, sets uWorld to identity matrix (default true)</param>
    public void SetCamera(ICamera3D camera, bool setWorld = true)
    {
        ArgumentNullException.ThrowIfNull(camera);

        _gl.UseProgram(Handle);

        // Set view and projection matrices
        SetUniformIfExists("uView", camera.View);
        SetUniformIfExists("uProjection", camera.Projection);

        // Set world to identity if requested
        if (setWorld)
        {
            SetUniformIfExists("uWorld", Matrix4X4<float>.Identity);
        }

        // Set camera vectors if they exist in the shader
        SetUniformIfExists("uCameraPosition", camera.Position);
        SetUniformIfExists("uCameraForward", camera.Forward);
        SetUniformIfExists("uCameraRight", camera.Right);
        SetUniformIfExists("uCameraUp", camera.Up);
    }

    /// <summary>
    /// Sets the model/world matrix (uWorld or uModel) in the shader.
    /// </summary>
    /// <param name="modelMatrix">The world/model transformation matrix</param>
    public void SetModel(Matrix4X4<float> modelMatrix)
    {
        _gl.UseProgram(Handle);

        // Try both common naming conventions
        SetUniformIfExists("uWorld", modelMatrix);
        SetUniformIfExists("uModel", modelMatrix);
    }

    /// <summary>
    /// Sets the model matrix from a position vector (creates a translation matrix).
    /// </summary>
    /// <param name="position">The world position</param>
    public void SetModel(Vector3D<float> position)
    {
        var modelMatrix = Matrix4X4.CreateTranslation(position);
        SetModel(modelMatrix);
    }

    /// <summary>
    /// Helper method to set a uniform only if it exists in the shader (doesn't throw exception).
    /// </summary>
    private void SetUniformIfExists(string name, Matrix4X4<float> value)
    {
        try
        {
            int location = _gl.GetUniformLocation(Handle, name);

            if (location != -1)
            {
                unsafe
                {
                    _gl.UniformMatrix4(location, 1, false, (float*)&value);
                }
            }
        }
        catch
        {
            // Silently ignore if uniform doesn't exist
        }
    }

    /// <summary>
    /// Helper method to set a uniform only if it exists in the shader (doesn't throw exception).
    /// </summary>
    private void SetUniformIfExists(string name, Vector3D<float> value)
    {
        try
        {
            int location = _gl.GetUniformLocation(Handle, name);

            if (location != -1)
            {
                _gl.Uniform3(location, value.X, value.Y, value.Z);
            }
        }
        catch
        {
            // Silently ignore if uniform doesn't exist
        }
    }

    /// <summary>
    /// Sets a texture uniform by binding the texture to a specific texture unit.
    /// </summary>
    /// <param name="name">The name of the sampler uniform in the shader</param>
    /// <param name="textureHandle">The OpenGL texture handle</param>
    /// <param name="textureUnit">The texture unit to bind to (0-31, default is 0)</param>
    public void SetUniform(string name, uint textureHandle, int textureUnit = 0)
    {
        int location = GetUniformLocation(name);

        // Activate the texture unit
        _gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        CheckGLError($"ActiveTexture({textureUnit})");

        // Bind the texture
        _gl.BindTexture(TextureTarget.Texture2D, textureHandle);
        CheckGLError($"BindTexture({textureHandle})");

        // Set the uniform to the texture unit index
        _gl.UseProgram(Handle);
        _gl.Uniform1(location, textureUnit);
        CheckGLError($"SetUniform({name}, texture unit {textureUnit})");
    }

    /// <summary>
    /// Sets a texture uniform using a TextureTarget (for cubemaps, 3D textures, etc.)
    /// </summary>
    /// <param name="name">The name of the sampler uniform in the shader</param>
    /// <param name="textureHandle">The OpenGL texture handle</param>
    /// <param name="textureTarget">The texture target type</param>
    /// <param name="textureUnit">The texture unit to bind to (0-31, default is 0)</param>
    public void SetUniform(string name, uint textureHandle, TextureTarget textureTarget, int textureUnit = 0)
    {
        int location = GetUniformLocation(name);

        // Activate the texture unit
        _gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        CheckGLError($"ActiveTexture({textureUnit})");

        // Bind the texture
        _gl.BindTexture(textureTarget, textureHandle);
        CheckGLError($"BindTexture({textureHandle}, {textureTarget})");

        // Set the uniform to the texture unit index
        _gl.UseProgram(Handle);
        _gl.Uniform1(location, textureUnit);
        CheckGLError($"SetUniform({name}, {textureTarget} texture unit {textureUnit})");
    }

    public void DebugUniforms()
    {
        _gl.GetProgram(Handle, GLEnum.ActiveUniforms, out var uniformCount);
        _logger.Information("Active uniforms: {Count}", uniformCount);

        for (uint i = 0; i < uniformCount; i++)
        {
            string name = _gl.GetActiveUniform(Handle, i, out _, out _);
            int location = _gl.GetUniformLocation(Handle, name);
            _logger.Information("Uniform '{Name}' at location {Location}", name, location);
        }
    }

    private static Dictionary<ShaderType, string> ParseShaderSource(string source)
    {
        var shaders = new Dictionary<ShaderType, string>();

        // Split by shader type markers
        var lines = source.Split('\n');
        ShaderType? currentType = null;
        var currentShader = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("#shader vertex", StringComparison.OrdinalIgnoreCase))
            {
                if (currentType.HasValue && currentShader.Count > 0)
                {
                    shaders[currentType.Value] = string.Join('\n', currentShader);
                }
                currentType = ShaderType.VertexShader;
                currentShader.Clear();
            }
            else if (line.StartsWith("#shader fragment", StringComparison.OrdinalIgnoreCase))
            {
                if (currentType.HasValue && currentShader.Count > 0)
                {
                    shaders[currentType.Value] = string.Join('\n', currentShader);
                }
                currentType = ShaderType.FragmentShader;
                currentShader.Clear();
            }
            else if (currentType.HasValue)
            {
                currentShader.Add(line);
            }
        }

        // Add the last shader
        if (currentType.HasValue && currentShader.Count > 0)
        {
            shaders[currentType.Value] = string.Join('\n', currentShader);
        }

        return shaders;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint handle = _gl.CreateShader(type);
        _gl.ShaderSource(handle, source);
        _gl.CompileShader(handle);

        string infoLog = _gl.GetShaderInfoLog(handle);

        return !string.IsNullOrWhiteSpace(infoLog) ? throw new ShaderCompilationException(type, source, infoLog) : handle;
    }

    private void CacheUniforms()
    {
        _gl.GetProgram(Handle, GLEnum.ActiveUniforms, out var uniformCount);
        _logger.Debug("Shader {Handle} - Active uniforms: {Count}", Handle, uniformCount);

        for (uint i = 0; i < uniformCount; i++)
        {
            var key = _gl.GetActiveUniform(Handle, i, out _, out _);
            _logger.Debug("Shader {Handle} - Uniform name: {Name}", Handle, key);

            var location = _gl.GetUniformLocation(Handle, key);
            _uniformLocations.Add(key, location);
        }
    }

    private void CheckGLError(string context = null)
    {
        var error = _gl.GetError();

        if (error != GLEnum.NoError)
        {
            _logger.Error("OpenGL Error: {Error} (Context: {Context})", error, context ?? "Unknown");

            throw new OpenGLException(error, context);
        }
    }

    ~OpenGlLillyShader()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (Handle != 0)
                {
                    _gl.DeleteProgram(Handle);
                }
            }

            _disposed = true;
        }
    }
}
