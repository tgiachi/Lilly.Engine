# Audio System

Lilly.Engine includes a 3D spatial audio system built on OpenAL. It supports playing sound effects (one-shot), music streams (looping), and positioning sounds in 3D space relative to the camera/listener.

## Overview

The audio system is managed by the `IAudioService`. It handles:

- **Listener Management**: Position and orientation of the "ears" in the virtual world.
- **Sound Effects**: Short audio clips loaded into memory (e.g., gunshots, footsteps).
- **Audio Streams**: Longer audio files streamed from disk (e.g., background music).
- **Spatial Audio**: Attenuation and panning based on distance and direction.

## Basic Usage

### Loading Audio

Before playing audio, you must load it using the `IAssetManager` or directly via `IAudioService`.

```csharp
public class MyScene : BaseScene
{
    private readonly IAudioService _audio;

    public MyScene(IAudioService audio)
    {
        _audio = audio;
    }

    public override void Initialize()
    {
        // Load a sound effect (wav, small files)
        _audio.LoadSoundEffect("jump", "Assets/Audio/jump.wav");

        // Load music stream (ogg/mp3, large files)
        _audio.LoadAudioStream("music", "Assets/Audio/music.ogg", AudioType.Ogg, isLooping: true);
    }
}
```

### Playing Sounds

```csharp
// Play a UI sound (no position)
_audio.PlaySoundEffect("click");

// Play music
_audio.PlayStream("music", volume: 0.5f);

// Stop music
_audio.StopAudio("music");
```

## 3D Spatial Audio

To make sounds appear to come from a specific location, use the 3D playback methods.

### 1. Update the Listener

For 3D audio to work, the engine needs to know where the "listener" (usually the player or camera) is. The `AudioService` provides a helper to sync with the camera:

```csharp
public override void Update(float deltaTime)
{
    // Sync listener with active camera
    _audio.Update(Camera);
}
```

Or manually:

```csharp
_audio.SetListenerPosition(player.Position);
_audio.SetListenerOrientation(player.Forward, player.Up);
_audio.SetListenerVelocity(player.Velocity); // For Doppler effect
```

### 2. Play 3D Sounds

```csharp
// Play sound at a specific position
_audio.PlaySoundEffect3D(
    "explosion",
    new Vector3(10, 0, 5),
    volume: 1.0f,
    referenceDistance: 5.0f
);
```

- **Position**: Where the sound comes from.
- **Reference Distance**: The distance at which the volume is 1.0 (max). As the listener moves away, volume decreases.

### 3D Streams (Ambient Sound)

You can also have spatial loops, like a waterfall or machinery:

```csharp
_audio.PlayStream3D(
    "waterfall",
    new Vector3(50, 0, 50),
    isLooping: true,
    referenceDistance: 10.0f
);
```

## Supported Formats

- **WAV**: Uncompressed, best for short sound effects.
- **OGG**: Compressed, best for music and long rendering.
- **MP3**: Supported but OGG is preferred for looping.

## Scripting

Audio functions are exposed to Lua scripts via the `audio` module (if available) or through game objects.

*(Note: Check `lua-scripting.md` or `definitions.lua` for the exact Lua API availability).*
