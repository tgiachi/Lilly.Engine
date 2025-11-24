namespace Lilly.Engine.Core.Data.Services;

/// <summary>
/// Represents a registration for a service that should start automatically.
/// </summary>
/// <param name="ServiceType">The type of the service to auto-start.</param>
public record AutostartRegistration(Type ServiceType);
