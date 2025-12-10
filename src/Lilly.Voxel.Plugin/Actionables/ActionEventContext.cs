using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Voxel.Plugin.Interfaces.Actionables;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Actionables;

/// <summary>
/// Context information for an action event occurring in the voxel world.
/// </summary>
public readonly record struct ActionEventContext
{
    public ActionEventType Event { get; init; }
    public Vector3 WorldPosition { get; init; }
    public GameTime? GameTime { get; init; }
    public IActionableTarget? Target { get; init; }
}
