namespace Lilly.Engine.Core.Data.Commands;

/// <summary>
/// Defines a parameter for a command, including its name, description, and type.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Description">A description of the parameter.</param>
/// <param name="ParameterType">The type of the parameter.</param>
public record CommandParameter(string Name, string Description, Type ParameterType);

