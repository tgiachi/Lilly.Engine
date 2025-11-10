using System.Runtime.InteropServices;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Commands;

/// <summary>
/// Represents a rendering command with a type and optional payload data.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderCommand
{
    /// <summary>
    /// Gets the type of rendering command.
    /// </summary>
    public RenderCommandType CommandType { get; init; }

    /// <summary>
    /// Gets the optional data payload associated with this command.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Initializes a new instance of the RenderCommand struct.
    /// </summary>
    /// <param name="commandType">The type of rendering command.</param>
    /// <param name="data">Optional data payload for the command.</param>
    public RenderCommand(RenderCommandType commandType, object? data = null)
    {
        CommandType = commandType;
        Data = data;
    }

    /// <summary>
    /// Gets the payload data cast to the specified type.
    /// </summary>
    /// <typeparam name="TPayload">The type to cast the payload to.</typeparam>
    /// <returns>The payload cast to the specified type, or default if the cast fails.</returns>
    public TPayload GetPayload<TPayload>() where TPayload : struct
        => Data is TPayload ? (TPayload)Data : default;

    public override string ToString() => $"RenderCommand: {CommandType}";
}
