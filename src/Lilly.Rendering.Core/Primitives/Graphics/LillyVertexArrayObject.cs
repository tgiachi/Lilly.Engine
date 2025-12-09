using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace Lilly.Rendering.Core.Primitives.Graphics;

public class LillyVertexArrayObject<TVertexType, TIndexType> : IDisposable
    where TVertexType : unmanaged
    where TIndexType : unmanaged
{
    public uint Handle { get; private set; }
    private readonly GL _gl;
    private bool disposed;

    public LillyVertexArrayObject(GL gl, LillyBufferObject<TVertexType> vbo, LillyBufferObject<TIndexType>? ebo = null)
    {
        _gl = gl;

        Handle = _gl.GenVertexArray();
        Bind();
        vbo.Bind(BufferTargetARB.ArrayBuffer);
        ebo?.Bind(BufferTargetARB.ElementArrayBuffer);
    }

    public void Bind()
    {
        _gl.BindVertexArray(Handle);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (Handle != 0)
        {
            _gl.DeleteVertexArray(Handle);
            Handle = 0;
        }

        disposed = true;
        GC.SuppressFinalize(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset<TF>(ref TVertexType vertex, ref TF field)
        => Unsafe.ByteOffset(ref Unsafe.As<TVertexType, byte>(ref vertex), ref Unsafe.As<TF, byte>(ref field)).ToInt32();

    /**
     * <summary>
     * Bind a vertex attribute to a field of the vertex type
     * only the integer types are accepted
     * </summary>
     * <param name="index">The index of the vertex attribute</param>
     * <param name="count">The number of components of the vertex attribute</param>
     * <param name="type">The type of the vertex attribute</param>
     * <param name="fieldName">The name of the field of the vertex type</param>
     */
    public void VertexAttributeIPointer(uint index, int count, VertexAttribIType type, string fieldName)
        => VertexAttributeIPointer(index, count, type, (int)Marshal.OffsetOf<TVertexType>(fieldName));

    public unsafe void VertexAttributeIPointer(uint index, int count, VertexAttribIType type, int offset)
    {
        _gl.VertexAttribIPointer(index, count, type, (uint)sizeof(TVertexType), (void*)offset);
        _gl.EnableVertexAttribArray(index);
    }

    /**
     * <summary>
     * Bind a vertex attribute to a field of the vertex type
     * the type of the field is converted to a float vector
     * </summary>
     * <param name="index">The index of the vertex attribute</param>
     * <param name="count">The number of components of the vertex attribute</param>
     * <param name="type">The type of the vertex attribute</param>
     * <param name="fieldName">The name of the field of the vertex type</param>
     */
    public void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, string fieldName)
        => VertexAttributePointer(index, count, type, (int)Marshal.OffsetOf<TVertexType>(fieldName));

    public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, int offset)
    {
        _gl.VertexAttribPointer(index, count, type, false, (uint)sizeof(TVertexType), (void*)offset);
        _gl.EnableVertexAttribArray(index);
    }
}
