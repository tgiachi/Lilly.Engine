using System.Buffers.Binary;
using NVorbis;
using Silk.NET.Maths;
using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AudioEffect : IDisposable
{
    private AL al;
    private readonly AlSource alSource;
    private readonly AlBuffer alBuffer;

    public AudioEffect(string path)
    {
        al = AudioMaster.GetInstance().Al;
        var vorbisReader = new VorbisReader(path);

        var channels = vorbisReader.Channels;
        var sampleRate = vorbisReader.SampleRate;
        var format = BufferFormat.Mono16;

        if (channels == 2)
        {
            format = BufferFormat.Stereo16;
        }

        alSource = new();
        alBuffer = new();

        var readBuffer = new float[channels * vorbisReader.TotalSamples];
        var rawData = new Span<byte>(new byte[readBuffer.Length * sizeof(short)]);
        var samplesRead = vorbisReader.ReadSamples(readBuffer, 0, readBuffer.Length);

        for (var i = 0; i < samplesRead; i++)
        {
            var sampleShort = (short)(readBuffer[i] * short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(rawData.Slice(i * sizeof(short), sizeof(short)), sampleShort);
        }

        alBuffer.SetData(format, rawData, sampleRate);
        alSource.SetBuffer(alBuffer);
    }

    public void Dispose()
    {
        alSource.Dispose();
        alBuffer.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Play()
    {
        alSource.Stop();
        alSource.Play();
    }

    /// <summary>
    /// Sets the volume of the sound effect. Range: 0.0 to 1.0+
    /// </summary>
    public void SetVolume(float volume)
    {
        alSource.SetProperty(SourceFloat.Gain, Math.Max(0.0f, volume));
    }

    /// <summary>
    /// Sets the 3D position of the sound effect.
    /// </summary>
    public void SetPosition(Vector3D<float> position)
    {
        alSource.SetPosition(position);
    }

    /// <summary>
    /// Sets the reference distance for 3D audio (distance at which volume = 100%).
    /// </summary>
    public void SetReferenceDistance(float distance)
    {
        alSource.SetReferenceDistance(distance);
    }

    /// <summary>
    /// Sets the maximum distance at which the sound can be heard.
    /// </summary>
    public void SetMaxDistance(float distance)
    {
        alSource.SetMaxDistance(distance);
    }

    /// <summary>
    /// Sets the rolloff factor (how quickly volume decreases with distance).
    /// Default is 1.0. Higher values = faster volume decrease.
    /// </summary>
    public void SetRolloffFactor(float rolloff)
    {
        alSource.SetRolloffFactor(rolloff);
    }

    /// <summary>
    /// Sets the velocity of the sound effect (for Doppler effect).
    /// </summary>
    public void SetVelocity(Vector3D<float> velocity)
    {
        alSource.SetVelocity(velocity);
    }
}
