using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Payloads;

/// <summary>
/// Payload for configuring depth buffer state.
/// </summary>
public readonly struct SetDepthStatePayload
{
    /// <summary>
    /// Gets whether depth testing is enabled.
    /// </summary>
    public bool DepthTestEnabled { get; init; }

    /// <summary>
    /// Gets whether writing to the depth buffer is enabled.
    /// </summary>
    public bool DepthWriteEnabled { get; init; }

    /// <summary>
    /// Gets the depth comparison function.
    /// </summary>
    public DepthFunction DepthFunction { get; init; }

    public SetDepthStatePayload(bool depthTestEnabled, bool depthWriteEnabled, DepthFunction depthFunction = Types.DepthFunction.Less)
    {
        DepthTestEnabled = depthTestEnabled;
        DepthWriteEnabled = depthWriteEnabled;
        DepthFunction = depthFunction;
    }

    /// <summary>
    /// Creates a depth state suitable for skybox rendering (test enabled, write disabled, LessEqual).
    /// </summary>
    public static SetDepthStatePayload SkyboxDepthState()
        => new(true, false, Types.DepthFunction.LessEqual);

    /// <summary>
    /// Creates the default depth state (test enabled, write enabled, Less).
    /// </summary>
    public static SetDepthStatePayload DefaultDepthState()
        => new(true, true, Types.DepthFunction.Less);
}
