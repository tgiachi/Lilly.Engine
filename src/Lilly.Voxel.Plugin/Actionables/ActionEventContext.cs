using System.Numerics;
using Lilly.Engine.Core.Data.Privimitives;
using Lilly.Voxel.Plugin.Blocks;
using Lilly.Voxel.Plugin.Types;

namespace Lilly.Voxel.Plugin.Actionables;

/// <summary>
/// Context information for an action event occurring in the voxel world.
/// </summary>
/// <param name="Event"></param>
/// <param name="WorldPosition"></param>
/// <param name="GameTime"></param>
/// <param name="Instance"></param>
public readonly record struct ActionEventContext(
    ActionEventType Event,
    Vector3 WorldPosition,
    GameTime GameTime,
    BlockInstance? Instance = null
);
