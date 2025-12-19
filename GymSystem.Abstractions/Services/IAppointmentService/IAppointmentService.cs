using GymSystem.Application.Abstractions.Services.IAppointmentService.Contract;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services.IAppointmentService;

public interface IAppointmentService : IApplicationService {
    Task<ServiceResponse<List<AppointmentDto>>> GetAllAsync();
    Task<ServiceResponse<AppointmentDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<AppointmentDto>> CreateAsync(AppointmentDto dto);
    Task<ServiceResponse<AppointmentDto>> UpdateAsync(int id, AppointmentDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
    Task<ServiceResponse<bool>> CheckTrainerAvailabilityAsync(int trainerId, DateTime appointmentDate, int durationMinutes);
    Task<ServiceResponse<bool>> CheckMemberAvailabilityAsync(int memberId, DateTime appointmentDate, int durationMinutes);
    Task<ServiceResponse<AppointmentDto>> BookAppointmentAsync(AppointmentDto dto);
    Task<ServiceResponse<AppointmentDto>> ConfirmAppointmentAsync(int appointmentId);
    Task<ServiceResponse<bool>> CancelAppointmentAsync(int appointmentId, string? reason);
    Task<ServiceResponse<List<AppointmentDto>>> GetMemberAppointmentsAsync(int memberId);
    Task<ServiceResponse<List<AppointmentDto>>> GetTrainerAppointmentsAsync(int trainerId);
    Task<ServiceResponse<List<int>>> GetAvailableTrainersAsync(int serviceId, DateTime appointmentDate, int durationMinutes);
}