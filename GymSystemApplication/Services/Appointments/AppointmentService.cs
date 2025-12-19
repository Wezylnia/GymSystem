using AutoMapper;
using GymSystem.Application.Abstractions.Services.IAppointmentService;
using GymSystem.Application.Abstractions.Services.IAppointmentService.Contract;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Appointments;

/// <summary>
/// Randevu servisi - DTO + AutoMapper + ServiceResponse pattern
/// </summary>
public class AppointmentService : IAppointmentService {
    private readonly BaseFactory<AppointmentService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IMapper _mapper;

    public AppointmentService(BaseFactory<AppointmentService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<List<AppointmentDto>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointments = await repository.QueryNoTracking()
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                    .ThenInclude(s => s!.GymLocation)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<AppointmentDto>>(appointments);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Tüm randevular getirilirken hata oluştu");
            return _responseHelper.SetError<List<AppointmentDto>>(
                null,
                new ErrorInfo("Randevular getirilemedi", "APPOINTMENT_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AppointmentDto?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.QueryNoTracking()
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                    .ThenInclude(s => s!.GymLocation)
                .Where(a => a.Id == id && a.IsActive)
                .FirstOrDefaultAsync();

            if (appointment == null)
                return _responseHelper.SetError<AppointmentDto?>(null, "Randevu bulunamadı", 404, "APPOINTMENT_NOTFOUND");

            var dto = _mapper.Map<AppointmentDto>(appointment);
            return _responseHelper.SetSuccess<AppointmentDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<AppointmentDto?>(null, new ErrorInfo("Randevu getirilemedi", "APPOINTMENT_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AppointmentDto>> CreateAsync(AppointmentDto dto) {
        try {
            var appointment = _mapper.Map<Appointment>(dto, opts => opts.AfterMap((src, dest) => {
                dest.Status = AppointmentStatus.Pending;
                dest.CreatedAt = DateTimeHelper.Now;
                dest.IsActive = true;
            }));

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            await repository.AddAsync(appointment);
            await repository.SaveChangesAsync();

            var responseDto = _mapper.Map<AppointmentDto>(appointment);
            return _responseHelper.SetSuccess(responseDto, "Randevu oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu");
            return _responseHelper.SetError<AppointmentDto>(null, new ErrorInfo("Randevu oluşturulamadı", "APPOINTMENT_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AppointmentDto>> UpdateAsync(int id, AppointmentDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.Query().Where(a => a.Id == id && a.IsActive).FirstOrDefaultAsync();

            if (appointment == null)
                return _responseHelper.SetError<AppointmentDto>(null, "Randevu bulunamadı", 404, "APPOINTMENT_NOTFOUND");

            _mapper.Map(dto, appointment);
            appointment.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(appointment);
            await repository.SaveChangesAsync();

            var responseDto = _mapper.Map<AppointmentDto>(appointment);
            return _responseHelper.SetSuccess(responseDto, "Randevu güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<AppointmentDto>(null, new ErrorInfo("Randevu güncellenemedi", "APPOINTMENT_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.Query().Where(a => a.Id == id && a.IsActive).FirstOrDefaultAsync();

            if (appointment == null)
                return _responseHelper.SetError<bool>(false, "Randevu bulunamadı", 404, "APPOINTMENT_NOTFOUND");

            appointment.IsActive = false;
            appointment.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(appointment);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Randevu silindi. ID: {Id}", id);
            return _responseHelper.SetSuccess(true, "Randevu silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Randevu silinemedi", "APPOINTMENT_DELETE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> CheckTrainerAvailabilityAsync(int trainerId, DateTime appointmentDate, int durationMinutes) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointmentEndTime = appointmentDate.AddMinutes(durationMinutes);

            var hasConflict = await repository.QueryNoTracking()
                .Where(a => a.TrainerId == trainerId && a.IsActive && a.Status != AppointmentStatus.Cancelled &&
                    ((appointmentDate >= a.AppointmentDate && appointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    (appointmentEndTime > a.AppointmentDate && appointmentEndTime <= a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    (appointmentDate <= a.AppointmentDate && appointmentEndTime >= a.AppointmentDate.AddMinutes(a.DurationMinutes))))
                .AnyAsync();

            if (hasConflict)
                return _responseHelper.SetError<bool>(false, "Antrenörün seçilen saatte başka bir randevusu var.", 400, "APPOINTMENT_001");

            var availabilityRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<TrainerAvailability>();
            var dayOfWeek = appointmentDate.DayOfWeek;
            var appointmentTime = appointmentDate.TimeOfDay;
            var appointmentEndTimeSpan = appointmentEndTime.TimeOfDay;

            var hasAvailability = await availabilityRepository.QueryNoTracking()
                .Where(a => a.TrainerId == trainerId && a.DayOfWeek == dayOfWeek && a.IsActive && a.StartTime <= appointmentTime && a.EndTime >= appointmentEndTimeSpan)
                .AnyAsync();

            if (!hasAvailability)
                return _responseHelper.SetError<bool>(false, $"Antrenör {dayOfWeek} günü {appointmentTime:hh\\:mm} - {appointmentEndTimeSpan:hh\\:mm} saatleri arasında müsait değil.", 400, "APPOINTMENT_002");

            return _responseHelper.SetSuccess(true, "Antrenör müsait");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör müsaitliği kontrol edilirken hata oluştu");
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Müsaitlik kontrolü başarısız", "APPOINTMENT_003", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> CheckMemberAvailabilityAsync(int memberId, DateTime appointmentDate, int durationMinutes) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointmentEndTime = appointmentDate.AddMinutes(durationMinutes);

            var hasConflict = await repository.QueryNoTracking()
                .Where(a => a.MemberId == memberId && a.IsActive && a.Status != AppointmentStatus.Cancelled &&
                    ((appointmentDate >= a.AppointmentDate && appointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    (appointmentEndTime > a.AppointmentDate && appointmentEndTime <= a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    (appointmentDate <= a.AppointmentDate && appointmentEndTime >= a.AppointmentDate.AddMinutes(a.DurationMinutes))))
                .AnyAsync();

            if (hasConflict)
                return _responseHelper.SetError<bool>(false, "Bu saatte başka bir randevunuz var.", 400, "APPOINTMENT_004");

            return _responseHelper.SetSuccess(true, "Üye müsait");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye müsaitliği kontrol edilirken hata oluştu");
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Müsaitlik kontrolü başarısız", "APPOINTMENT_005", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AppointmentDto>> BookAppointmentAsync(AppointmentDto dto) {
        try {
            var trainerAvailability = await CheckTrainerAvailabilityAsync(dto.TrainerId, dto.AppointmentDate, dto.DurationMinutes);
            if (!trainerAvailability.IsSuccessful)
                return _responseHelper.SetError<AppointmentDto>(null, trainerAvailability.Error?.ErrorMessage ?? "Antrenör müsait değil", 400, trainerAvailability.Error?.ErrorCode ?? "APPOINTMENT_006");

            var memberAvailability = await CheckMemberAvailabilityAsync(dto.MemberId, dto.AppointmentDate, dto.DurationMinutes);
            if (!memberAvailability.IsSuccessful)
                return _responseHelper.SetError<AppointmentDto>(null, memberAvailability.Error?.ErrorMessage ?? "Bu saatte başka randevunuz var", 400, memberAvailability.Error?.ErrorCode ?? "APPOINTMENT_007");

            var serviceRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var service = await serviceRepository.QueryNoTracking()
                .Where(s => s.Id == dto.ServiceId)
                .Select(s => new { s.Id, s.GymLocationId })
                .FirstOrDefaultAsync();

            if (service == null)
                return _responseHelper.SetError<AppointmentDto>(null, "Hizmet bulunamadı", 404, "APPOINTMENT_008");

            var workingHoursRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<WorkingHours>();
            var dayOfWeek = dto.AppointmentDate.DayOfWeek;
            var appointmentTime = dto.AppointmentDate.TimeOfDay;
            var appointmentEndTime = dto.AppointmentDate.AddMinutes(dto.DurationMinutes).TimeOfDay;

            var isOpen = await workingHoursRepository.QueryNoTracking()
                .Where(w => w.GymLocationId == service.GymLocationId && w.DayOfWeek == dayOfWeek && w.IsActive && !w.IsClosed && w.OpenTime <= appointmentTime && w.CloseTime >= appointmentEndTime)
                .AnyAsync();

            if (!isOpen)
                return _responseHelper.SetError<AppointmentDto>(null, "Salon seçilen gün ve saatte kapalı", 400, "APPOINTMENT_009");

            return await CreateAsync(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu");
            return _responseHelper.SetError<AppointmentDto>(null, new ErrorInfo("Randevu oluşturulamadı", "APPOINTMENT_010", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<AppointmentDto>> ConfirmAppointmentAsync(int appointmentId) {
        try {
            var appointmentResponse = await GetByIdAsync(appointmentId);

            if (!appointmentResponse.IsSuccessful || appointmentResponse.Data == null)
                return _responseHelper.SetError<AppointmentDto>(null, "Randevu bulunamadı", 404, "APPOINTMENT_011");

            if (appointmentResponse.Data.Status != AppointmentStatus.Pending.ToString())
                return _responseHelper.SetError<AppointmentDto>(null, "Sadece bekleyen randevular onaylanabilir", 400, "APPOINTMENT_012");

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.Query().Where(a => a.Id == appointmentId).FirstOrDefaultAsync();

            if (appointment == null)
                return _responseHelper.SetError<AppointmentDto>(null, "Randevu bulunamadı", 404, "APPOINTMENT_011");

            appointment.Status = AppointmentStatus.Confirmed;
            appointment.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(appointment);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Randevu onaylandı. AppointmentId: {AppointmentId}", appointmentId);

            var responseDto = _mapper.Map<AppointmentDto>(appointment);
            return _responseHelper.SetSuccess(responseDto, "Randevu onaylandı");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu onaylanırken hata oluştu");
            return _responseHelper.SetError<AppointmentDto>(null, new ErrorInfo("Randevu onaylanamadı", "APPOINTMENT_013", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> CancelAppointmentAsync(int appointmentId, string? reason) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.Query().Where(a => a.Id == appointmentId && a.IsActive).FirstOrDefaultAsync();

            if (appointment == null)
                return _responseHelper.SetError<bool>(false, "Randevu bulunamadı", 404, "APPOINTMENT_014");

            if (appointment.Status == AppointmentStatus.Cancelled)
                return _responseHelper.SetError<bool>(false, "Randevu zaten iptal edilmiş", 400, "APPOINTMENT_015");

            if (appointment.Status == AppointmentStatus.Completed)
                return _responseHelper.SetError<bool>(false, "Tamamlanmış randevular iptal edilemez", 400, "APPOINTMENT_016");

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTimeHelper.Now;
            if (!string.IsNullOrEmpty(reason))
                appointment.Notes = $"İptal Nedeni: {reason}";

            await repository.UpdateAsync(appointment);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Randevu iptal edildi. AppointmentId: {AppointmentId}", appointmentId);
            return _responseHelper.SetSuccess(true, "Randevu iptal edildi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Randevu iptal edilirken hata oluştu");
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Randevu iptal edilemedi", "APPOINTMENT_017", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<AppointmentDto>>> GetMemberAppointmentsAsync(int memberId) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointments = await repository.QueryNoTracking()
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                    .ThenInclude(s => s!.GymLocation)
                .Where(a => a.MemberId == memberId && a.IsActive)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<AppointmentDto>>(appointments);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye randevuları getirilirken hata oluştu");
            return _responseHelper.SetError<List<AppointmentDto>>(null, new ErrorInfo("Randevular getirilemedi", "APPOINTMENT_018", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<AppointmentDto>>> GetTrainerAppointmentsAsync(int trainerId) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointments = await repository.QueryNoTracking()
                .Include(a => a.Member)
                .Include(a => a.Trainer)
                .Include(a => a.Service)
                    .ThenInclude(s => s!.GymLocation)
                .Where(a => a.TrainerId == trainerId && a.IsActive)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            var dtos = _mapper.Map<List<AppointmentDto>>(appointments);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Antrenör randevuları getirilirken hata oluştu");
            return _responseHelper.SetError<List<AppointmentDto>>(null, new ErrorInfo("Randevular getirilemedi", "APPOINTMENT_019", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<int>>> GetAvailableTrainersAsync(int serviceId, DateTime appointmentDate, int durationMinutes) {
        try {
            var specialtyRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<TrainerSpecialty>();

            var trainerIds = await specialtyRepository.QueryNoTracking()
                .Where(ts => ts.ServiceId == serviceId && ts.IsActive)
                .Select(ts => ts.TrainerId)
                .Distinct()
                .ToListAsync();

            var availableTrainerIds = new List<int>();

            foreach (var trainerId in trainerIds) {
                var availability = await CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);
                if (availability.IsSuccessful && availability.Data == true)
                    availableTrainerIds.Add(trainerId);
            }

            return _responseHelper.SetSuccess(availableTrainerIds);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Uygun antrenörler getirilirken hata oluştu");
            return _responseHelper.SetError<List<int>>(null, new ErrorInfo("Uygun antrenörler getirilemedi", "APPOINTMENT_020", ex.StackTrace, 500));
        }
    }
}