using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

/// <summary>
/// Randevu servisi - Generic CRUD + Custom business logic
/// </summary>
public interface IAppointmentService : IGenericCrudService<Appointment>, IApplicationService
{
    /// <summary>
    /// Belirli bir tarih ve saatte antrenörün müsait olup olmadığını kontrol eder
    /// </summary>
    Task<ServiceResponse<bool>> CheckTrainerAvailabilityAsync(int trainerId, DateTime appointmentDate, int durationMinutes);
    
    /// <summary>
    /// Üyenin belirli bir tarihte randevusu var mı kontrol eder
    /// </summary>
    Task<ServiceResponse<bool>> CheckMemberAvailabilityAsync(int memberId, DateTime appointmentDate, int durationMinutes);
    
    /// <summary>
    /// Randevu oluşturur (tüm kontroller ile)
    /// </summary>
    Task<ServiceResponse<Appointment>> BookAppointmentAsync(Appointment appointment);
    
    /// <summary>
    /// Randevu onaylar (Admin/Trainer)
    /// </summary>
    Task<ServiceResponse<Appointment>> ConfirmAppointmentAsync(int appointmentId);
    
    /// <summary>
    /// Randevu iptal eder
    /// </summary>
    Task<ServiceResponse<bool>> CancelAppointmentAsync(int appointmentId, string? reason);
    
    /// <summary>
    /// Belirli bir üyenin tüm randevularını getirir
    /// </summary>
    Task<ServiceResponse<IEnumerable<Appointment>>> GetMemberAppointmentsAsync(int memberId);
    
    /// <summary>
    /// Belirli bir antrenörün tüm randevularını getirir
    /// </summary>
    Task<ServiceResponse<IEnumerable<Appointment>>> GetTrainerAppointmentsAsync(int trainerId);
    
    /// <summary>
    /// Belirli bir tarihte uygun antrenörleri getirir
    /// </summary>
    Task<ServiceResponse<IEnumerable<int>>> GetAvailableTrainersAsync(int serviceId, DateTime appointmentDate, int durationMinutes);
}
