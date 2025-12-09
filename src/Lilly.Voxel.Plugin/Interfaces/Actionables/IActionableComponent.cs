using System.Text.Json.Serialization;
using Lilly.Voxel.Plugin.Actionables.Components;

namespace Lilly.Voxel.Plugin.Interfaces.Actionables;

/// <summary>
/// Marker interface for components that provide actionable behavior to blocks.
/// </summary>
[
    JsonPolymorphic(TypeDiscriminatorPropertyName = "$type"),
    JsonDerivedType(typeof(SoundComponent), "sound"),
    JsonDerivedType(typeof(NotificationComponent), "notification"),
    JsonDerivedType(typeof(LightComponent), "light")
]
public interface IActionableComponent;
