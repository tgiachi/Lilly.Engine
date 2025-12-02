namespace Lilly.Engine.Core.Data.Commands;

/// <summary>
/// Represents a registration entry for a command type.
/// </summary>
/// <param name="CommandType">The type of the command to register.</param>
public record CommandDefinitionRegistration(Type CommandType);
