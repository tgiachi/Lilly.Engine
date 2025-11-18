using Silk.NET.Maths;
using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AlSource : IDisposable
{
    private readonly AL al;
    public readonly uint sourcehandle;

    public AlSource()
    {
        sourcehandle = AudioMaster.GetInstance().Al.GenSource();
        al = AudioMaster.GetInstance().Al;
    }

    public void Dispose()
    {
        al.DeleteSource(sourcehandle);
        GC.SuppressFinalize(this);
    }

    public int GetProcessedBuffers()
    {
        al.GetSourceProperty(sourcehandle, GetSourceInteger.BuffersProcessed, out var processed);

        return processed;
    }

    public void Play()
    {
        al.SourcePlay(sourcehandle);
    }

    public void QueueBuffers(AlBuffer[] buffers)
    {
        al.SourceQueueBuffers(sourcehandle, buffers.Select(a => a.bufferhandle).ToArray());
    }

    public void SetBuffer(AlBuffer buffer)
    {
        al.SetSourceProperty(sourcehandle, SourceInteger.Buffer, buffer.bufferhandle);
    }

    public void SetProperty(SourceBoolean looping, bool b)
    {
        al.SetSourceProperty(sourcehandle, looping, b);
    }

    public void SetProperty(SourceFloat gain, float b)
    {
        al.SetSourceProperty(sourcehandle, gain, b);
    }

    public void Stop()
    {
        al.SourceStop(sourcehandle);
    }

    public void UnqueueBuffer(AlBuffer[] buffers)
    {
        al.SourceUnqueueBuffers(sourcehandle, buffers.Select(a => a.bufferhandle).ToArray());
    }

    /// <summary>
    /// Sets the 3D position of the audio source.
    /// </summary>
    /// <param name="position">The position in 3D space.</param>
    public void SetPosition(Vector3D<float> position)
    {
        al.SetSourceProperty(sourcehandle, SourceVector3.Position, position.X, position.Y, position.Z);
    }

    /// <summary>
    /// Sets the velocity of the audio source (for Doppler effect).
    /// </summary>
    /// <param name="velocity">The velocity in 3D space.</param>
    public void SetVelocity(Vector3D<float> velocity)
    {
        al.SetSourceProperty(sourcehandle, SourceVector3.Velocity, velocity.X, velocity.Y, velocity.Z);
    }

    /// <summary>
    /// Sets the reference distance for the audio source (distance at which volume = 100%).
    /// </summary>
    /// <param name="distance">The reference distance in units.</param>
    public void SetReferenceDistance(float distance)
    {
        al.SetSourceProperty(sourcehandle, SourceFloat.ReferenceDistance, distance);
    }

    /// <summary>
    /// Sets the maximum distance at which the source can be heard.
    /// </summary>
    /// <param name="distance">The maximum distance in units.</param>
    public void SetMaxDistance(float distance)
    {
        al.SetSourceProperty(sourcehandle, SourceFloat.MaxDistance, distance);
    }

    /// <summary>
    /// Sets the rolloff factor (how quickly volume decreases with distance).
    /// Default is 1.0. Higher values = faster volume decrease.
    /// </summary>
    /// <param name="rolloff">The rolloff factor.</param>
    public void SetRolloffFactor(float rolloff)
    {
        al.SetSourceProperty(sourcehandle, SourceFloat.RolloffFactor, rolloff);
    }

}
