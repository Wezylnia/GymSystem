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
    
    /// <summary>
    /// Salon sahipleri için detaylı istatistikler
    /// </summary>
    Task<ServiceResponse<object>> GetGymOwnerDashboardStatsAsync(int? gymLocationId = null);
    
    /// <summary>
    /// Üyelik istatistikleri (onaylanmış, bekleyen, reddedilen)
    /// </summary>
    Task<ServiceResponse<object>> GetMembershipStatisticsAsync(int? gymLocationId = null);
    
    /// <summary>
    /// Salonlara göre gelir dağılımı
    /// </summary>
    Task<ServiceResponse<object>> GetRevenueByGymLocationAsync();
    
    /// <summary>
    /// Son 6 ay gelir trendi
    /// </summary>
    Task<ServiceResponse<object>> GetRevenueTrendAsync(int? gymLocationId = null);
    
    /// <summary>
    /// Üye artış trendi (aylık)
    /// </summary>
    Task<ServiceResponse<object>> GetMemberGrowthTrendAsync(int? gymLocationId = null);
}
