using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Payloads.GpuSubCommands;

/// <summary>
/// Payload for configuring face culling mode.
/// </summary>
public readonly struct SetCullMode
{
    /// <summary>
    /// Gets the culling mode.
    /// </summary>
    public CullFaceMode CullMode { get; init; }

    public SetCullMode(CullFaceMode cullMode)
    {
        CullMode = cullMode;
    }

    /// <summary>
    /// Creates a cull mode with no culling (suitable for skybox).
    /// </summary>
    public static SetCullMode None()
        => new(CullFaceMode.None);

    /// <summary>
    /// Creates a cull mode with back-face culling (default).
    /// </summary>
    public static SetCullMode Back()
        => new(CullFaceMode.Back);

    /// <summary>
    /// Creates a cull mode with front-face culling.
    /// </summary>
    public static SetCullMode Front()
        => new(CullFaceMode.Front);
}
