namespace Lilly.Engine.Core.Data.Commands;

/// <summary>
/// Represents the result of executing a command, including success status, message, and optional exception.
/// </summary>
/// <param name="Success">Indicates whether the command executed successfully.</param>
/// <param name="Message">A message describing the result or error.</param>
/// <param name="Exception">An optional exception that occurred during execution.</param>
public record CommandExecuteResult(bool Success, string Message, Exception? Exception);

