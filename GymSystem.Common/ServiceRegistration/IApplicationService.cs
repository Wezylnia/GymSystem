namespace GymSystem.Common.ServiceRegistration;

/// <summary>
/// Marker interface for application services.
/// Services implementing this interface will be registered as Scoped by default.
/// </summary>
public interface IApplicationService : IService {
}
