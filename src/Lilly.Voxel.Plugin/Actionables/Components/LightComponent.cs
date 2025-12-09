using Lilly.Voxel.Plugin.Interfaces.Actionables;
using TrippyGL;

namespace Lilly.Voxel.Plugin.Actionables.Components;

/// <summary>
/// Represents a light-emitting action component that can be associated with a block or entity.
/// </summary>
/// <param name="Color"></param>
/// <param name="Radius"></param>
public readonly record struct LightComponent(Color4b Color, int Radius) : IActionableComponent;
