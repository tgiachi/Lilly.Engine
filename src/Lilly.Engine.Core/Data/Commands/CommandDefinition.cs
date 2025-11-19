namespace Lilly.Engine.Core.Data.Commands;

public record CommandDefinition(
    string Name,
    string Description,
    string[]? Aliases
);
