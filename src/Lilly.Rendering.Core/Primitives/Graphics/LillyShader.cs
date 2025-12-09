using System.Buffers;
using System.Numerics;
using Lilly.Rendering.Core.Exceptions;
using Silk.NET.OpenGL;

namespace Lilly.Rendering.Core.Primitives.Graphics;

public class LillyShader : IDisposable
{
    public uint Handle { get; }

    private readonly GL gl;
    private readonly string _vertexLabel;
    private readonly string _fragmentLabel;
    private bool disposed;
    private readonly Dictionary<string, int> uniformLocations = new();

    public LillyShader(GL gl, string vertexPath, string fragmentPath)
        : this(
            gl,
            File.ReadAllText(vertexPath),
            File.ReadAllText(fragmentPath),
            vertexPath,
            fragmentPath
        ) { }

    public LillyShader(
        GL gl,
        string vertexSource,
        string fragmentSource,
        string? vertexLabel = null,
        string? fragmentLabel = null
    )
    {
        this.gl = gl;
        _vertexLabel = vertexLabel ?? "vertex_shader";
        _fragmentLabel = fragmentLabel ?? "fragment_shader";

        var vertex = LoadShaderSource(ShaderType.VertexShader, vertexSource, _vertexLabel);
        var fragment = LoadShaderSource(ShaderType.FragmentShader, fragmentSource, _fragmentLabel);
        Handle = this.gl.CreateProgram();
        this.gl.AttachShader(Handle, vertex);
        this.gl.AttachShader(Handle, fragment);
        this.gl.LinkProgram(Handle);
        this.gl.GetProgram(Handle, GLEnum.LinkStatus, out var status);
        var programLog = this.gl.GetProgramInfoLog(Handle);

        if (status == 0)
        {
            throw new ShaderLinkException(_vertexLabel, _fragmentLabel, programLog);
        }
        this.gl.DetachShader(Handle, vertex);
        this.gl.DetachShader(Handle, fragment);
        this.gl.DeleteShader(vertex);
        this.gl.DeleteShader(fragment);
    }

    public void BindUniformBlock(string name, uint bindingIndex)
    {
        var blockIndex = gl.GetUniformBlockIndex(Handle, name);

        if (blockIndex == uint.MaxValue)
        {
            throw new UniformNotFoundException(name);
        }

        gl.UniformBlockBinding(Handle, blockIndex, bindingIndex);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public uint GetUniformBlockIndex(string name)
        => gl.GetUniformBlockIndex(Handle, name);

    public int GetUniformLocation(string name)
    {
        if (!uniformLocations.TryGetValue(name, out var location))
        {
            location = gl.GetUniformLocation(Handle, name);

            if (location == -1)
            {
                throw new UniformNotFoundException(name);
            }
            uniformLocations.Add(name, location);
        }

        return uniformLocations[name];
    }

    public void SetUniform(string name, int value)
        => gl.Uniform1(GetUniformLocation(name), value);

    public void SetUniform(string name, uint value)
        => gl.Uniform1(GetUniformLocation(name), (int)value);

    public void SetUniform(string name, bool value)
        => gl.Uniform1(GetUniformLocation(name), value ? 1 : 0);

    public void SetUniform(string name, Vector2 value)
        => gl.Uniform2(GetUniformLocation(name), value.X, value.Y);

    public void SetUniform(string name, Vector4 value)
        => gl.Uniform4(GetUniformLocation(name), value.X, value.Y, value.Z, value.W);

    public void SetUniform(string name, ReadOnlySpan<int> values)
    {
        if (values.IsEmpty)
        {
            return;
        }
        var location = GetUniformLocation(name);

        unsafe
        {
            fixed (int* ptr = values)
            {
                gl.Uniform1(location, (uint)values.Length, ptr);
            }
        }
    }

    public void SetUniform(string name, ReadOnlySpan<float> values)
    {
        if (values.IsEmpty)
        {
            return;
        }
        var location = GetUniformLocation(name);

        unsafe
        {
            fixed (float* ptr = values)
            {
                gl.Uniform1(location, (uint)values.Length, ptr);
            }
        }
    }

    public void SetUniform(string name, ReadOnlySpan<Vector2> values)
        => SetUniformVectorArray(name, values, 2);

    public void SetUniform(string name, ReadOnlySpan<Vector3> values)
        => SetUniformVectorArray(name, values, 3);

    public void SetUniform(string name, ReadOnlySpan<Vector4> values)
        => SetUniformVectorArray(name, values, 4);

    public unsafe void SetUniform(string name, Matrix4x4 value)
        => gl.UniformMatrix4(GetUniformLocation(name), 1, false, (float*)&value);

    public unsafe void SetUniform(string name, ReadOnlySpan<Matrix4x4> values)
    {
        if (values.IsEmpty)
        {
            return;
        }
        var location = GetUniformLocation(name);

        fixed (Matrix4x4* ptr = values)
        {
            gl.UniformMatrix4(location, (uint)values.Length, false, (float*)ptr);
        }
    }

    public void SetUniform(string name, float value)
        => gl.Uniform1(GetUniformLocation(name), value);

    public void SetUniform(string name, Vector3 value)
        => gl.Uniform3(GetUniformLocation(name), value.X, value.Y, value.Z);

    public void Use()
    {
        gl.UseProgram(Handle);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (Handle != 0)
                {
                    gl.DeleteProgram(Handle);
                }
            }
            disposed = true;
        }
    }

    private static void FillFlatten<T>(ReadOnlySpan<T> values, int componentsPerElement, Span<float> target)
    {
        switch (componentsPerElement)
        {
            case 2 when typeof(T) == typeof(Vector2):
                for (var i = 0; i < values.Length; i++)
                {
                    var v = (Vector2)(object)values[i]!;
                    target[i * 2 + 0] = v.X;
                    target[i * 2 + 1] = v.Y;
                }

                break;
            case 3 when typeof(T) == typeof(Vector3):
                for (var i = 0; i < values.Length; i++)
                {
                    var v = (Vector3)(object)values[i]!;
                    target[i * 3 + 0] = v.X;
                    target[i * 3 + 1] = v.Y;
                    target[i * 3 + 2] = v.Z;
                }

                break;
            case 4 when typeof(T) == typeof(Vector4):
                for (var i = 0; i < values.Length; i++)
                {
                    var v = (Vector4)(object)values[i]!;
                    target[i * 4 + 0] = v.X;
                    target[i * 4 + 1] = v.Y;
                    target[i * 4 + 2] = v.Z;
                    target[i * 4 + 3] = v.W;
                }

                break;
            default:
                throw new NotSupportedException(
                    $"Unsupported vector flatten of {typeof(T)} with {componentsPerElement} components."
                );
        }
    }

    private uint LoadShaderSource(ShaderType type, string source, string label)
    {
        var handle = gl.CreateShader(type);
        gl.ShaderSource(handle, source);
        gl.CompileShader(handle);
        gl.GetShader(handle, GLEnum.CompileStatus, out var status);
        var infoLog = gl.GetShaderInfoLog(handle);

        // Only fail on actual compile errors; non-empty log may contain warnings.
        if (status == 0)
        {
            throw new ShaderCompilationException(type, label, infoLog);
        }

        return handle;
    }

    private unsafe void SetUniformVectorArray<T>(string name, ReadOnlySpan<T> values, int componentsPerElement)
        where T : struct
    {
        if (values.IsEmpty)
        {
            return;
        }

        var location = GetUniformLocation(name);
        var floatCount = values.Length * componentsPerElement;

        const int stackThreshold = 256;
        float[]? rented = null;
        var buffer = floatCount <= stackThreshold
                         ? stackalloc float[stackThreshold]
                         : (rented = ArrayPool<float>.Shared.Rent(floatCount)).AsSpan(0, floatCount);

        var target = buffer[..floatCount];
        FillFlatten(values, componentsPerElement, target);

        fixed (float* ptr = target)
        {
            switch (componentsPerElement)
            {
                case 2:
                    gl.Uniform2(location, (uint)values.Length, ptr);

                    break;
                case 3:
                    gl.Uniform3(location, (uint)values.Length, ptr);

                    break;
                case 4:
                    gl.Uniform4(location, (uint)values.Length, ptr);

                    break;
                default:
                    throw new NotSupportedException($"Unsupported component count {componentsPerElement}.");
            }
        }

        if (rented is not null)
        {
            ArrayPool<float>.Shared.Return(rented);
        }
    }
}
