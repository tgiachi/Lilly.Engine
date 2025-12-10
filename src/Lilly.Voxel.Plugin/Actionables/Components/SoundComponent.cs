using Lilly.Voxel.Plugin.Interfaces.Actionables;

namespace Lilly.Voxel.Plugin.Actionables.Components;

/// <summary>
/// Represents a sound action component that can be associated with a block or entity.
/// </summary>
/// <param name="soundId"></param>
public record SoundComponent(string SoundId) : IActionableComponent;
