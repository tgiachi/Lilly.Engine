using TrippyGL;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload for configuring blend state.
/// </summary>
public readonly struct SetBlendStatePayload
{
    /// <summary>
    /// Gets the blend state to set.
    /// </summary>
    public BlendState BlendState { get; init; }

    public SetBlendStatePayload(BlendState blendState)
    {
        BlendState = blendState;
    }

    /// <summary>
    /// Creates a blend state for alpha transparency.
    /// </summary>
    public static SetBlendStatePayload AlphaBlend()
        => new(BlendState.AlphaBlend);

    /// <summary>
    /// Creates a blend state for opaque rendering.
    /// </summary>
    public static SetBlendStatePayload Opaque()
        => new(BlendState.Opaque);
}
