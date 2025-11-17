using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload for configuring face culling mode.
/// </summary>
public readonly struct SetCullModePayload
{
    /// <summary>
    /// Gets the culling mode.
    /// </summary>
    public CullFaceMode CullMode { get; init; }

    public SetCullModePayload(CullFaceMode cullMode)
    {
        CullMode = cullMode;
    }

    /// <summary>
    /// Creates a cull mode with no culling (suitable for skybox).
    /// </summary>
    public static SetCullModePayload None()
        => new(CullFaceMode.None);

    /// <summary>
    /// Creates a cull mode with back-face culling (default).
    /// </summary>
    public static SetCullModePayload Back()
        => new(CullFaceMode.Back);

    /// <summary>
    /// Creates a cull mode with front-face culling.
    /// </summary>
    public static SetCullModePayload Front()
        => new(CullFaceMode.Front);
}
