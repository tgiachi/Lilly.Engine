using Silk.NET.Maths;
using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AlSource : IDisposable
{
    private readonly AL al;
    public uint SourceHandle { get; }

    public AlSource()
    {
        SourceHandle = AudioMaster.GetInstance().Al.GenSource();
        al = AudioMaster.GetInstance().Al;
    }

    public void Dispose()
    {
        al.DeleteSource(SourceHandle);
        GC.SuppressFinalize(this);
    }

    public int GetProcessedBuffers()
    {
        al.GetSourceProperty(SourceHandle, GetSourceInteger.BuffersProcessed, out var processed);

        return processed;
    }

    public void Play()
    {
        al.SourcePlay(SourceHandle);
    }

    public void QueueBuffers(AlBuffer[] buffers)
    {
        al.SourceQueueBuffers(SourceHandle, buffers.Select(a => a.bufferhandle).ToArray());
    }

    public void SetBuffer(AlBuffer buffer)
    {
        al.SetSourceProperty(SourceHandle, SourceInteger.Buffer, buffer.bufferhandle);
    }

    public void SetProperty(SourceBoolean looping, bool b)
    {
        al.SetSourceProperty(SourceHandle, looping, b);
    }

    public void SetProperty(SourceFloat gain, float b)
    {
        al.SetSourceProperty(SourceHandle, gain, b);
    }

    public void Stop()
    {
        al.SourceStop(SourceHandle);
    }

    public void UnqueueBuffer(AlBuffer[] buffers)
    {
        al.SourceUnqueueBuffers(SourceHandle, buffers.Select(a => a.bufferhandle).ToArray());
    }

    /// <summary>
    /// Sets the 3D position of the audio source.
    /// </summary>
    /// <param name="position">The position in 3D space.</param>
    public void SetPosition(Vector3D<float> position)
    {
        al.SetSourceProperty(SourceHandle, SourceVector3.Position, position.X, position.Y, position.Z);
    }

    /// <summary>
    /// Sets the velocity of the audio source (for Doppler effect).
    /// </summary>
    /// <param name="velocity">The velocity in 3D space.</param>
    public void SetVelocity(Vector3D<float> velocity)
    {
        al.SetSourceProperty(SourceHandle, SourceVector3.Velocity, velocity.X, velocity.Y, velocity.Z);
    }

    /// <summary>
    /// Sets the reference distance for the audio source (distance at which volume = 100%).
    /// </summary>
    /// <param name="distance">The reference distance in units.</param>
    public void SetReferenceDistance(float distance)
    {
        al.SetSourceProperty(SourceHandle, SourceFloat.ReferenceDistance, distance);
    }

    /// <summary>
    /// Sets the maximum distance at which the source can be heard.
    /// </summary>
    /// <param name="distance">The maximum distance in units.</param>
    public void SetMaxDistance(float distance)
    {
        al.SetSourceProperty(SourceHandle, SourceFloat.MaxDistance, distance);
    }

    /// <summary>
    /// Sets the rolloff factor (how quickly volume decreases with distance).
    /// Default is 1.0. Higher values = faster volume decrease.
    /// </summary>
    /// <param name="rolloff">The rolloff factor.</param>
    public void SetRolloffFactor(float rolloff)
    {
        al.SetSourceProperty(SourceHandle, SourceFloat.RolloffFactor, rolloff);
    }
}
