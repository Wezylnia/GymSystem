using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Infastructure.ServiceRegistration;

/// <summary>
/// DEPRECATED: Bu interface artık Common'da.
/// Geriye uyumluluk için burada bırakıldı.
/// Lütfen GymSystem.Common.ServiceRegistration.IApplicationService kullanın.
/// </summary>
[Obsolete("Use GymSystem.Common.ServiceRegistration.IApplicationService instead")]
public interface IApplicationService : Common.ServiceRegistration.IApplicationService
{
}
