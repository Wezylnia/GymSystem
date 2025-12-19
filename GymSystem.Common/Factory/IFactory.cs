using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Common.Factory;

/// <summary>
/// Factory marker interface - IApplicationService'den türer
/// Otomatik service registration için kullanılır
/// </summary>
public interface IFactory : IApplicationService {
}