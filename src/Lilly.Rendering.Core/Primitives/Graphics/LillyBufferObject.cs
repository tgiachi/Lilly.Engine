using Silk.NET.OpenGL;

namespace Lilly.Rendering.Core.Primitives.Graphics;

public class LillyBufferObject<TDataType> : IDisposable
    where TDataType : unmanaged
{
    public uint Handle { get; }
    private readonly BufferTargetARB bufferType;
    private readonly GL gl;
    private bool disposed;

    public unsafe LillyBufferObject(
        GL gl,
        Span<TDataType> data,
        BufferTargetARB bufferType,
        BufferUsageARB usage = BufferUsageARB.StaticDraw
    )
    {
        this.gl = gl;
        this.bufferType = bufferType;

        Handle = this.gl.GenBuffer();
        Bind(bufferType);

        fixed (void* d = data)
        {
            this.gl.BufferData(bufferType, (nuint)(data.Length * sizeof(TDataType)), d, usage);
        }
    }

    public unsafe LillyBufferObject(
        GL gl,
        int nbVertex,
        BufferTargetARB bufferType,
        BufferUsageARB bufferUsageArb = BufferUsageARB.DynamicCopy
    )
    {
        this.gl = gl;
        this.bufferType = bufferType;

        Handle = gl.GenBuffer();
        Bind(bufferType);
        gl.BufferData(bufferType, (nuint)(nbVertex * sizeof(TDataType)), null, bufferUsageArb);
    }

    ~LillyBufferObject()
    {
        Dispose(false);
    }

    public void Bind(BufferTargetARB bufferTargetType)
    {
        gl.BindBuffer(bufferTargetType, Handle);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public unsafe TDataType GetData()
    {
        gl.GetNamedBufferSubData<TDataType>(Handle, 0, (uint)sizeof(TDataType), out var countCompute);

        return countCompute;
    }

    public void SendData(ReadOnlySpan<TDataType> data, nint offset)
    {
        Bind(bufferType);
        gl.BufferSubData(bufferType, offset, data);
    }

    protected void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                gl.DeleteBuffer(Handle);
            }
            disposed = true;
        }
    }
}
