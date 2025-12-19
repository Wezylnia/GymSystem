using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Infastructure.ServiceRegistration;

/// <summary>
/// DEPRECATED: Bu interface artık Common'da.
/// Geriye uyumluluk için burada bırakıldı.
/// Lütfen GymSystem.Common.ServiceRegistration.IService kullanın.
/// </summary>
[Obsolete("Use GymSystem.Common.ServiceRegistration.IService instead")]
public interface IService : Common.ServiceRegistration.IService {
}
