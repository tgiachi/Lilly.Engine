using System.Numerics;
using Lilly.Engine.Audio;
using Lilly.Rendering.Core.Interfaces.Camera;

namespace Lilly.Engine.Interfaces.Services;

/// <summary>
/// Interface for audio service providing 3D spatial audio capabilities.
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// Gets the current listener position.
    /// </summary>
    Vector3 GetListenerPosition();

    /// <summary>
    /// Loads an audio stream from file.
    /// </summary>
    AudioStream LoadAudioStream(
        string streamName,
        string filePath,
        AudioType audioType = AudioType.Ogg,
        bool isLooping = true
    );

    /// <summary>
    /// Loads an audio stream from a stream.
    /// </summary>
    AudioStream LoadAudioStream(
        string streamName,
        Stream stream,
        AudioType audioType = AudioType.Ogg,
        bool isLooping = true
    );

    /// <summary>
    /// Loads a sound effect from file.
    /// </summary>
    void LoadSoundEffect(string soundName, string filePath, AudioType audioType = AudioType.Ogg);

    /// <summary>
    /// Loads a sound effect from a stream.
    /// </summary>
    void LoadSoundEffect(string soundName, Stream stream, AudioType audioType = AudioType.Ogg);

    /// <summary>
    /// Plays a sound effect at the listener's position (non-spatial).
    /// </summary>
    void PlaySoundEffect(string soundName, float volume = 1.0f);

    /// <summary>
    /// Plays a specific sound effect instance at the listener's position (non-spatial).
    /// </summary>
    void PlaySoundEffect(AudioEffect effect, float volume = 1.0f);

    /// <summary>
    /// Plays a sound effect at a specific position in 3D space.
    /// </summary>
    void PlaySoundEffect3D(string soundName, Vector3 position, float volume = 1.0f, float referenceDistance = 1.0f);

    /// <summary>
    /// Plays a specific sound effect instance at a position in 3D space.
    /// </summary>
    void PlaySoundEffect3D(
        AudioEffect effect,
        Vector3 position,
        float volume = 1.0f,
        float referenceDistance = 1.0f
    );

    /// <summary>
    /// Plays a looping audio stream.
    /// </summary>
    void PlayStream(string streamName, float volume = 1.0f, bool isLooping = true);

    /// <summary>
    /// Plays a looping audio stream at a specific position in 3D space.
    /// </summary>
    void PlayStream3D(
        string streamName,
        Vector3 position,
        float volume = 1.0f,
        bool isLooping = true,
        float referenceDistance = 1.0f
    );

    /// <summary>
    /// Sets the listener's orientation (forward and up vectors).
    /// </summary>
    void SetListenerOrientation(Vector3 forward, Vector3 up);

    /// <summary>
    /// Sets the listener's position in 3D space.
    /// </summary>
    void SetListenerPosition(Vector3 position);

    /// <summary>
    /// Sets the listener's velocity (for Doppler effect).
    /// </summary>
    void SetListenerVelocity(Vector3 velocity);

    /// <summary>
    /// Stops all audio playback.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Stops a stream.
    /// </summary>
    void StopAudio(string soundName);

    /// <summary>
    /// Sets the camera to synchronize listener position and orientation.
    /// </summary>
    void Update(ICamera3D camera);
}
