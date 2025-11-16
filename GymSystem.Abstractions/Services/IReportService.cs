using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services;

public interface IReportService : IApplicationService
{
    /// <summary>
    /// Belirli bir uzmanlık alanındaki antrenörleri getir
    /// </summary>
    Task<ServiceResponse<object>> GetTrainersBySpecialtyAsync(string specialty);
    
    /// <summary>
    /// Belirli bir tarih ve saatte uygun antrenörleri getir
    /// </summary>
    Task<ServiceResponse<object>> GetAvailableTrainersWithDetailsAsync(int serviceId, DateTime appointmentDateTime, int durationMinutes);
    
    /// <summary>
    /// Belirli bir üyenin tüm randevularını getir
    /// </summary>
    Task<ServiceResponse<object>> GetMemberAppointmentsWithDetailsAsync(int memberId);
    
    /// <summary>
    /// En popüler hizmetleri getir
    /// </summary>
    Task<ServiceResponse<object>> GetPopularServicesAsync(int top = 5);
    
    /// <summary>
    /// Aylık gelir raporu
    /// </summary>
    Task<ServiceResponse<object>> GetMonthlyRevenueAsync(int month, int year);
    
    /// <summary>
    /// Antrenör iş yükü raporu
    /// </summary>
    Task<ServiceResponse<object>> GetTrainerWorkloadAsync(int? trainerId = null);
}
