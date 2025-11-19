namespace Lilly.Engine.Core.Data.Commands;

public record CommandExecuteResult(bool Success, string Message, Exception? Exception);

