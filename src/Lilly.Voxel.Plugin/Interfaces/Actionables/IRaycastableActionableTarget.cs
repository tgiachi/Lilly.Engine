using System.Numerics;
using Lilly.Rendering.Core.Primitives;

namespace Lilly.Voxel.Plugin.Interfaces.Actionables;

/// <summary>
/// Actionable target that can be hit-tested via raycast (e.g., models).
/// </summary>
public interface IRaycastableActionableTarget : IActionableTarget
{
    bool Raycast(Ray ray, float maxDistance, out float distance, out Vector3 hitPoint);
}
