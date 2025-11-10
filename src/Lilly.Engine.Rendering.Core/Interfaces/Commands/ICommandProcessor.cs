using Lilly.Engine.Rendering.Core.Commands;
using Lilly.Engine.Rendering.Core.Types;

namespace Lilly.Engine.Rendering.Core.Interfaces.Commands;

/// <summary>
/// Defines a processor for a specific type of render command.
/// Each processor handles the execution of commands of its specific type.
/// </summary>
public interface ICommandProcessor
{
    /// <summary>
    /// Gets the type of command this processor handles.
    /// </summary>
    RenderCommandType CommandType { get; }

    /// <summary>
    /// Processes a single render command.
    /// This method should be highly optimized as it's called frequently during rendering.
    /// </summary>
    /// <param name="command">The render command to process.</param>
    void Process(RenderCommand command);
}
