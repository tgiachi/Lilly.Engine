namespace Lilly.Engine.Core.Interfaces.Services.Base;

/// <summary>
/// Represents a base service interface in the Lilly Engine.
/// </summary>
public interface ILillyService
{
    Task StartAsync();

    Task ShutdownAsync();
}
