using GymSystem.Application.Abstractions.Services.IBodyMeasurement.Contract;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services.IBodyMeasurement;

/// <summary>
/// Boy-Kilo takip servisi interface
/// </summary>
public interface IBodyMeasurementService : IApplicationService {
    /// <summary>
    /// Üyenin tüm ölçümlerini getirir (tarihe göre sýralý)
    /// </summary>
    Task<ServiceResponse<List<BodyMeasurementDto>>> GetMemberMeasurementsAsync(int memberId);
    
    /// <summary>
    /// Belirli bir ölçümü getirir
    /// </summary>
    Task<ServiceResponse<BodyMeasurementDto?>> GetByIdAsync(int id);
    
    /// <summary>
    /// Yeni ölçüm ekler
    /// </summary>
    Task<ServiceResponse<BodyMeasurementDto>> CreateAsync(BodyMeasurementDto dto);
    
    /// <summary>
    /// Ölçüm günceller
    /// </summary>
    Task<ServiceResponse<BodyMeasurementDto>> UpdateAsync(BodyMeasurementDto dto);
    
    /// <summary>
    /// Ölçüm siler (soft delete)
    /// </summary>
    Task<ServiceResponse<bool>> DeleteAsync(int id);
    
    /// <summary>
    /// Grafik verileri için ölçümleri getirir
    /// </summary>
    Task<ServiceResponse<List<BodyMeasurementDto>>> GetChartDataAsync(int memberId, DateTime? startDate = null, DateTime? endDate = null);
}
