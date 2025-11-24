using System.Buffers.Binary;
using System.Numerics;
using NVorbis;
using Silk.NET.Maths;
using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AudioStream : IDisposable
{
    public const int NB_BUFFERS = 4;
    private readonly VorbisReader vorbisReader;
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly Queue<AlBuffer> buffers = new();
    private readonly AL al;
    private readonly AlSource alSource;
    private bool isPlaying = true;
    private readonly BufferFormat _format;
    private bool disposed;
    private bool loop;
    private Thread? thread;

    public AudioStream(string path, bool loop = true)
    {
        this.loop = loop;
        al = AudioMaster.GetInstance().Al;
        vorbisReader = new(path);

        _channels = vorbisReader.Channels;
        _sampleRate = vorbisReader.SampleRate;
        _format = BufferFormat.Mono16;

        if (_channels == 2)
        {
            _format = BufferFormat.Stereo16;
        }

        alSource = new();

        for (var i = 0; i < 4; i++)
        {
            var alBuffer = new AlBuffer();
            var haveNext = FillBufferWithSound(alBuffer, _channels, _sampleRate, _format);
            buffers.Enqueue(alBuffer);

            if (!haveNext)
            {
                break;
            }
        }
        alSource.QueueBuffers(buffers.ToArray());
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        alSource.Stop();
        isPlaying = false;
        vorbisReader.Dispose();
        alSource.Dispose();

        foreach (var buffer in buffers)
        {
            buffer.Dispose();
        }
        disposed = true;
        GC.SuppressFinalize(this);
    }

    public void Pause()
    {
        alSource.Stop();
    }

    public void Play()
    {
        alSource.Play();

        if (thread is null)
        {
            thread = new(ThreadUpdater);
            thread.Start();
        }
    }

    public void Seek(int i)
    {
        vorbisReader.SeekTo(i);
    }

    public void Stop()
        => Dispose();

    public void UpdateBuffer()
    {
        al.GetSourceProperty(
            alSource.SourceHandle,
            GetSourceInteger.BuffersProcessed,
            out var processedBuffersCount
        );

        while (processedBuffersCount-- > 0)
        {
            var buffer = buffers.Dequeue();
            alSource.UnqueueBuffer([buffer]);

            if (!FillBufferWithSound(buffer, _channels, _sampleRate, _format))
            {
                isPlaying = false;

                break;
            }
            alSource.QueueBuffers([buffer]);
            buffers.Enqueue(buffer);
        }
    }

    private bool FillBufferWithSound(AlBuffer buffer, int channels, int sampleRate, BufferFormat format)
    {
        var readBuffer = new float[channels * sampleRate / 5];
        var rawData = new Span<byte>(new byte[readBuffer.Length * sizeof(short)]);

        var samplesRead = vorbisReader.ReadSamples(readBuffer, 0, readBuffer.Length);

        for (var i = 0; i < samplesRead; i++)
        {
            var sampleShort = (short)(readBuffer[i] * short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(rawData.Slice(i * sizeof(short), sizeof(short)), sampleShort);
        }

        if (loop && samplesRead < readBuffer.Length)
        {
            vorbisReader.SeekTo(0);
            var samplesRead2 = vorbisReader.ReadSamples(readBuffer, samplesRead, readBuffer.Length - samplesRead);

            for (var i = samplesRead; i < samplesRead + samplesRead2; i++)
            {
                var sampleShort = (short)(readBuffer[i] * short.MaxValue);
                BinaryPrimitives.WriteInt16LittleEndian(rawData.Slice(i * sizeof(short), sizeof(short)), sampleShort);
            }
        }
        buffer.SetData(format, rawData.ToArray(), sampleRate);

        return loop || samplesRead >= readBuffer.Length;
    }

    private void ThreadUpdater()
    {
        while (isPlaying)
        {
            UpdateBuffer();
            Thread.Sleep(500);
        }
        Dispose();
    }

    /// <summary>
    /// Sets the volume of the audio stream. Range: 0.0 to 1.0+
    /// </summary>
    public void SetVolume(float volume)
    {
        alSource.SetProperty(SourceFloat.Gain, Math.Max(0.0f, volume));
    }

    /// <summary>
    /// Sets whether the audio stream loops.
    /// </summary>
    public void SetLooping(bool looping)
    {
        loop = looping;
    }

    /// <summary>
    /// Sets the 3D position of the audio stream.
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        alSource.SetPosition(new Vector3D<float>(position.X, position.Y, position.Z));
    }

    /// <summary>
    /// Sets the reference distance for 3D audio (distance at which volume = 100%).
    /// </summary>
    public void SetReferenceDistance(float distance)
    {
        alSource.SetReferenceDistance(distance);
    }

    /// <summary>
    /// Sets the maximum distance at which the stream can be heard.
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
    /// Sets the velocity of the audio stream (for Doppler effect).
    /// </summary>
    public void SetVelocity(Vector3 velocity)
    {
        alSource.SetVelocity(new Vector3D<float>(velocity.X, velocity.Y, velocity.Z));
    }
}
