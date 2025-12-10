using System.Numerics;
using System.Collections.Generic;
using System.IO;
using Lilly.Engine.Audio;
using Lilly.Engine.Interfaces.Services;
using Lilly.Rendering.Core.Interfaces.Camera;
using Serilog;
using Silk.NET.OpenAL;

namespace Lilly.Engine.Services;

/// <summary>
/// Service for managing audio playback with 3D spatial audio support.
/// Handles listener position, audio source management, and sound effects.
/// </summary>
public class AudioService : IAudioService
{
    private readonly ILogger _logger = Log.ForContext<AudioService>();
    private readonly AL _al;
    private Vector3 _listenerPosition = Vector3.Zero;
    private Vector3 _listenerVelocity = Vector3.Zero;
    private Vector3 _listenerForward = new(0, 0, -1);
    private Vector3 _listenerUp = new(0, 1, 0);

    private readonly Dictionary<string, AudioEffect> _soundEffects = new();
    private readonly Dictionary<string, AudioStream> _streams = new();
    private readonly Dictionary<string, AlBuffer> _cachedBuffers = new();
    private readonly List<string> _tempFiles = new();

    public AudioService()
    {
        /**
         *
         * // open the mp3 file.
MP3Stream stream = new MP3Stream(@"sample.mp3");
// Create the buffer.
byte[] buffer = new byte[4096];
// read the entire mp3 file.
int bytesReturned = 1;
int totalBytesRead = 0;
while (bytesReturned > 0)
{
    bytesReturned = stream.Read(buffer, 0, buffer.Length);
    totalBytesRead += bytesReturned;
}
// close the stream after we're done with it.
stream.Close();
         */
        // Initialize AudioMaster (OpenAL context)
        _al = AudioMaster.GetInstance().Al;

        // Set initial listener parameters
        UpdateListenerProperties();

        _logger.Information("Audio Service initialized");
    }

    public void Dispose()
    {
        try
        {
            StopAll();

            foreach (var effect in _soundEffects.Values)
            {
                effect.Dispose();
            }
            _soundEffects.Clear();

            foreach (var stream in _streams.Values)
            {
                stream.Dispose();
            }
            _streams.Clear();

            foreach (var buffer in _cachedBuffers.Values)
            {
                buffer.Dispose();
            }
            _cachedBuffers.Clear();

            foreach (var temp in _tempFiles)
            {
                try
                {
                    if (File.Exists(temp))
                    {
                        File.Delete(temp);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to delete temp audio file {Temp}", temp);
                }
            }
            _tempFiles.Clear();

            _logger.Information("Audio Service disposed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error disposing Audio Service");
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the current listener position.
    /// </summary>
    public Vector3 GetListenerPosition()
        => _listenerPosition;

    /// <summary>
    /// Loads an audio stream from file.
    /// </summary>
    public AudioStream LoadAudioStream(
        string streamName,
        string filePath,
        AudioType audioType = AudioType.Ogg,
        bool isLooping = true
    )
    {
        try
        {
            var stream = new AudioStream(filePath, audioType, isLooping);
            _streams[streamName] = stream;
            _logger.Information("Loaded audio stream '{StreamName}' from '{FilePath}'", streamName, filePath);
            return stream;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading audio stream '{StreamName}' from '{FilePath}'", streamName, filePath);
        }

        throw new InvalidOperationException($"Failed to load audio stream '{streamName}'");
    }

    private string CreateTempFileForAudio(Stream stream, AudioType audioType)
    {
        var extension = audioType switch
        {
            AudioType.Mp3 => ".mp3",
            _ => ".ogg"
        };

        var tempPath = Path.ChangeExtension(Path.GetTempFileName(), extension);

        using (var fs = File.Open(tempPath, FileMode.Create, FileAccess.Write))
        {
            stream.CopyTo(fs);
        }

        _tempFiles.Add(tempPath);
        stream.Position = 0;

        return tempPath;
    }

    public AudioStream LoadAudioStream(string streamName, Stream stream, AudioType audioType = AudioType.Ogg, bool isLooping = true)
    {
        var tempPath = CreateTempFileForAudio(stream, audioType);
        return LoadAudioStream(streamName, tempPath, audioType, isLooping);
    }

    /// <summary>
    /// Loads a sound effect from file.
    /// </summary>
    public void LoadSoundEffect(string soundName, string filePath, AudioType audioType = AudioType.Ogg)
    {
        try
        {
            var effect = new AudioEffect(filePath, audioType);
            _soundEffects[soundName] = effect;
            _logger.Information("Loaded sound effect '{SoundName}' from '{FilePath}'", soundName, filePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading sound effect '{SoundName}' from '{FilePath}'", soundName, filePath);
        }
    }

    public void LoadSoundEffect(string soundName, Stream stream, AudioType audioType = AudioType.Ogg)
    {
        var tempPath = CreateTempFileForAudio(stream, audioType);
        LoadSoundEffect(soundName, tempPath, audioType);
    }

    /// <summary>
    /// Plays a sound effect at the listener's position (non-spatial).
    /// </summary>
    public void PlaySoundEffect(string soundName, float volume = 1.0f)
    {
        try
        {
            if (!_soundEffects.TryGetValue(soundName, out var effect))
            {
                _logger.Warning("Sound effect '{SoundName}' not found", soundName);

                return;
            }

            effect.SetVolume(volume);
            effect.Play();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error playing sound effect '{SoundName}'", soundName);
        }
    }

    public void PlaySoundEffect(AudioEffect effect, float volume = 1.0f)
    {
        try
        {
            effect.SetVolume(volume);
            effect.Play();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error playing provided sound effect instance");
        }
    }

    /// <summary>
    /// Plays a sound effect at a specific position in 3D space.
    /// </summary>
    public void PlaySoundEffect3D(string soundName, Vector3 position, float volume = 1.0f, float referenceDistance = 1.0f)
    {
        try
        {
            if (!_soundEffects.TryGetValue(soundName, out var effect))
            {
                _logger.Warning("Sound effect '{SoundName}' not found", soundName);

                return;
            }

            effect.SetVolume(volume);
            effect.SetPosition(position);
            effect.SetReferenceDistance(referenceDistance);
            effect.Play();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error playing 3D sound effect '{SoundName}'", soundName);
        }
    }

    public void PlaySoundEffect3D(AudioEffect effect, Vector3 position, float volume = 1.0f, float referenceDistance = 1.0f)
    {
        try
        {
            effect.SetVolume(volume);
            effect.SetPosition(position);
            effect.SetReferenceDistance(referenceDistance);
            effect.Play();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error playing provided 3D sound effect instance");
        }
    }

    /// <summary>
    /// Plays a looping audio stream.
    /// </summary>
    public void PlayStream(string streamName, float volume = 1.0f, bool isLooping = true)
    {
        try
        {
            if (!_streams.TryGetValue(streamName, out var stream))
            {
                _logger.Warning("Audio stream '{StreamName}' not found", streamName);

                return;
            }

            stream.SetVolume(volume);
            stream.SetLooping(isLooping);
            stream.Play();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error playing stream '{StreamName}'", streamName);
        }
    }

    /// <summary>
    /// Plays a looping audio stream at a specific position in 3D space.
    /// </summary>
    public void PlayStream3D(
        string streamName,
        Vector3 position,
        float volume = 1.0f,
        bool isLooping = true,
        float referenceDistance = 1.0f
    )
    {
        try
        {
            if (!_streams.TryGetValue(streamName, out var stream))
            {
                _logger.Warning("Audio stream '{StreamName}' not found", streamName);

                return;
            }

            stream.SetVolume(volume);
            stream.SetLooping(isLooping);
            stream.SetPosition(position);
            stream.SetReferenceDistance(referenceDistance);
            stream.Play();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error playing 3D stream '{StreamName}'", streamName);
        }
    }

    /// <summary>
    /// Sets the listener's orientation (forward and up vectors).
    /// </summary>
    public void SetListenerOrientation(Vector3 forward, Vector3 up)
    {
        _listenerForward = forward;
        _listenerUp = up;
        UpdateListenerProperties();
    }

    /// <summary>
    /// Sets the listener's position in 3D space.
    /// </summary>
    public void SetListenerPosition(Vector3 position)
    {
        _listenerPosition = position;
        UpdateListenerProperties();
    }

    /// <summary>
    /// Sets the listener's velocity (for Doppler effect).
    /// </summary>
    public void SetListenerVelocity(Vector3 velocity)
    {
        _listenerVelocity = velocity;
        UpdateListenerProperties();
    }

    /// <summary>
    /// Stops all audio playback.
    /// </summary>
    public void StopAll()
    {
        foreach (var stream in _streams.Values)
        {
            stream.Stop();
        }
    }

    /// <summary>
    /// Stops a stream.
    /// </summary>
    public void StopAudio(string soundName)
    {
        try
        {
            if (_streams.TryGetValue(soundName, out var stream))
            {
                stream.Stop();

                return;
            }

            _logger.Warning("Stream '{SoundName}' not found", soundName);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error stopping stream '{SoundName}'", soundName);
        }
    }

    public void Update(ICamera3D camera)
    {
        SetListenerPosition(camera.Position);
        SetListenerOrientation(camera.Forward, camera.Up);
    }

    private void UpdateListenerProperties()
    {
        try
        {
            // Set listener position
            _al.SetListenerProperty(ListenerVector3.Position, _listenerPosition.X, _listenerPosition.Y, _listenerPosition.Z);

            // Set listener velocity
            _al.SetListenerProperty(ListenerVector3.Velocity, _listenerVelocity.X, _listenerVelocity.Y, _listenerVelocity.Z);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating listener properties");
        }
    }
}
