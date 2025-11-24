namespace Lilly.Engine.Core.Data.Commands;

/// <summary>
/// Defines a command with its name, description, and optional aliases.
/// </summary>
/// <param name="Name">The name of the command.</param>
/// <param name="Description">A description of what the command does.</param>
/// <param name="Aliases">Optional alternative names for the command.</param>
public record CommandDefinition(
    string Name,
    string Description,
    string[]? Aliases
);
