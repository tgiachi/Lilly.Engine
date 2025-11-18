using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AlBuffer : IDisposable
{
    internal readonly uint bufferhandle;
    private readonly AL al;

    public AlBuffer()
    {
        al = AudioMaster.GetInstance().Al;
        bufferhandle = al.GenBuffer();
    }

    public void Dispose()
    {
        al.DeleteBuffer(bufferhandle);
        GC.SuppressFinalize(this);
    }

    public unsafe void SetData(BufferFormat bufferFormat, ReadOnlySpan<byte> data, int frequency)
    {
        fixed (byte* ptr = data)
        {
            al.BufferData(bufferhandle, bufferFormat, ptr, data.Length, frequency);
        }
    }
}
