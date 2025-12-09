using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using MP3Sharp;
using NVorbis;
using Silk.NET.OpenAL;

namespace Lilly.Engine.Audio;

public class AudioStream : IDisposable
{
    public const int NB_BUFFERS = 4;

    private readonly IAudioDecoder decoder;
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

    public AudioStream(string path, AudioType audioType = AudioType.Ogg, bool loop = true)
    {
        this.loop = loop;
        al = AudioMaster.GetInstance().Al;

        var (initialStream, streamFactory) = CreateStreamFactory(path);
        decoder = CreateDecoder(audioType, initialStream, streamFactory);

        _channels = decoder.Channels;
        _sampleRate = decoder.SampleRate;
        _format = _channels == 2 ? BufferFormat.Stereo16 : BufferFormat.Mono16;

        alSource = new();

        for (var i = 0; i < NB_BUFFERS; i++)
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

    private interface IAudioDecoder : IDisposable
    {
        int Channels { get; }
        int SampleRate { get; }
        int ReadSamples(short[] buffer, int offset, int count);
        void Reset();
        void Seek(long samplePosition);
    }

    private sealed class OggDecoder : IAudioDecoder
    {
        private VorbisReader reader;
        private readonly Func<Stream> streamFactory;

        public OggDecoder(Stream stream, Func<Stream> streamFactory)
        {
            this.streamFactory = streamFactory;
            reader = CreateReader(stream);
        }

        public int Channels => reader.Channels;
        public int SampleRate => reader.SampleRate;

        public void Dispose()
        {
            reader.Dispose();
        }

        public int ReadSamples(short[] buffer, int offset, int count)
        {
            var floatBuffer = ArrayPool<float>.Shared.Rent(count);
            var read = reader.ReadSamples(floatBuffer, 0, count);

            for (var i = 0; i < read; i++)
            {
                var sample = Math.Clamp(floatBuffer[i], -1.0f, 1.0f);
                buffer[offset + i] = (short)(sample * short.MaxValue);
            }

            ArrayPool<float>.Shared.Return(floatBuffer);

            return read;
        }

        public void Reset()
        {
            try
            {
                reader.SeekTo(0);
            }
            catch
            {
                reader.Dispose();
                reader = CreateReader(streamFactory());
            }
        }

        public void Seek(long samplePosition)
        {
            try
            {
                reader.SeekTo(samplePosition);
            }
            catch
            {
                Reset();
            }
        }

        private static VorbisReader CreateReader(Stream stream)
            => new(stream);
    }

    private sealed class Mp3Decoder : IAudioDecoder
    {
        private MP3Stream mp3Stream;
        private Stream backingStream;
        private readonly Func<Stream> streamFactory;
        private byte[] byteBuffer = new byte[8192];

        public Mp3Decoder(Stream stream, Func<Stream> streamFactory)
        {
            this.streamFactory = streamFactory;
            backingStream = stream;
            mp3Stream = new(backingStream);
        }

        public int Channels => mp3Stream.ChannelCount;
        public int SampleRate => mp3Stream.Frequency;

        public void Dispose()
        {
            mp3Stream.Dispose();
            backingStream.Dispose();
        }

        public int ReadSamples(short[] buffer, int offset, int count)
        {
            var bytesNeeded = count * sizeof(short);

            if (byteBuffer.Length < bytesNeeded)
            {
                byteBuffer = new byte[bytesNeeded];
            }

            var bytesRead = mp3Stream.Read(byteBuffer, 0, Math.Min(byteBuffer.Length, bytesNeeded));
            var samplesRead = bytesRead / sizeof(short);

            for (var i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] =
                    BinaryPrimitives.ReadInt16LittleEndian(byteBuffer.AsSpan(i * sizeof(short), sizeof(short)));
            }

            return samplesRead;
        }

        public void Reset()
        {
            mp3Stream.Dispose();
            backingStream.Dispose();

            backingStream = streamFactory();
            mp3Stream = new(backingStream);
        }

        public void Seek(long samplePosition)
        {
            Reset();

            if (samplePosition <= 0)
            {
                return;
            }

            var skipBuffer = ArrayPool<short>.Shared.Rent(4096);
            var remaining = samplePosition;

            while (remaining > 0)
            {
                var toRead = (int)Math.Min(remaining, skipBuffer.Length);
                var read = ReadSamples(skipBuffer, 0, toRead);

                if (read == 0)
                {
                    break;
                }
                remaining -= read;
            }

            ArrayPool<short>.Shared.Return(skipBuffer);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        alSource.Stop();
        isPlaying = false;
        decoder.Dispose();
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
        decoder.Seek(i);
    }

    /// <summary>
    /// Sets whether the audio stream loops.
    /// </summary>
    public void SetLooping(bool looping)
    {
        loop = looping;
    }

    /// <summary>
    /// Sets the maximum distance at which the stream can be heard.
    /// </summary>
    public void SetMaxDistance(float distance)
    {
        alSource.SetMaxDistance(distance);
    }

    /// <summary>
    /// Sets the 3D position of the audio stream.
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        alSource.SetPosition(new(position.X, position.Y, position.Z));
    }

    /// <summary>
    /// Sets the reference distance for 3D audio (distance at which volume = 100%).
    /// </summary>
    public void SetReferenceDistance(float distance)
    {
        alSource.SetReferenceDistance(distance);
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
        alSource.SetVelocity(new(velocity.X, velocity.Y, velocity.Z));
    }

    /// <summary>
    /// Sets the volume of the audio stream. Range: 0.0 to 1.0+
    /// </summary>
    public void SetVolume(float volume)
    {
        alSource.SetProperty(SourceFloat.Gain, Math.Max(0.0f, volume));
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

    private static IAudioDecoder CreateDecoder(AudioType audioType, Stream initialStream, Func<Stream> streamFactory)
        => audioType switch
        {
            AudioType.Ogg => new OggDecoder(initialStream, streamFactory),
            AudioType.Mp3 => new Mp3Decoder(initialStream, streamFactory),
            _             => throw new ArgumentOutOfRangeException(nameof(audioType), audioType, "Unsupported audio format")
        };

    private static (Stream initialStream, Func<Stream> streamFactory) CreateStreamFactory(string source)
    {
        Func<Stream> fileFactory = () => File.Open(source, FileMode.Open, FileAccess.Read, FileShare.Read);

        return (fileFactory(), fileFactory);
    }

    private bool FillBufferWithSound(AlBuffer buffer, int channels, int sampleRate, BufferFormat format)
    {
        var samplesPerBuffer = channels * sampleRate / 5;
        var readBuffer = ArrayPool<short>.Shared.Rent(samplesPerBuffer);

        var samplesRead = decoder.ReadSamples(readBuffer, 0, samplesPerBuffer);

        if (loop && samplesRead < samplesPerBuffer)
        {
            decoder.Reset();
            samplesRead += decoder.ReadSamples(readBuffer, samplesRead, samplesPerBuffer - samplesRead);
        }

        if (samplesRead == 0)
        {
            ArrayPool<short>.Shared.Return(readBuffer);

            return false;
        }

        var rawData = new byte[samplesRead * sizeof(short)];
        var rawSpan = new Span<byte>(rawData);

        for (var i = 0; i < samplesRead; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(rawSpan.Slice(i * sizeof(short), sizeof(short)), readBuffer[i]);
        }

        buffer.SetData(format, rawData, sampleRate);
        ArrayPool<short>.Shared.Return(readBuffer);

        return loop || samplesRead >= samplesPerBuffer;
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
}
