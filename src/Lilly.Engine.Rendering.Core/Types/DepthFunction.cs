namespace Lilly.Engine.Rendering.Core.Types;

/// <summary>
/// Specifies the depth comparison function.
/// </summary>
public enum DepthFunction : byte
{
    /// <summary>
    /// Never passes.
    /// </summary>
    Never,

    /// <summary>
    /// Passes if the incoming value is less than the stored value.
    /// </summary>
    Less,

    /// <summary>
    /// Passes if the incoming value is equal to the stored value.
    /// </summary>
    Equal,

    /// <summary>
    /// Passes if the incoming value is less than or equal to the stored value.
    /// </summary>
    LessEqual,

    /// <summary>
    /// Passes if the incoming value is greater than the stored value.
    /// </summary>
    Greater,

    /// <summary>
    /// Passes if the incoming value is not equal to the stored value.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Passes if the incoming value is greater than or equal to the stored value.
    /// </summary>
    GreaterEqual,

    /// <summary>
    /// Always passes.
    /// </summary>
    Always
}
