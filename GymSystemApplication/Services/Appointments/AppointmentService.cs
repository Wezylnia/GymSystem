using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Models;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Appointments;

public class AppointmentService : GenericCrudService<Appointment>, IAppointmentService
{
    public AppointmentService(BaseFactory<GenericCrudService<Appointment>> baseFactory)
        : base(baseFactory)
    {
    }

    public async Task<ServiceResponse<bool>> CheckTrainerAvailabilityAsync(int trainerId, DateTime appointmentDate, int durationMinutes)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointmentEndTime = appointmentDate.AddMinutes(durationMinutes);

            // Antrenörün o saatte başka randevusu var mı?
            var existingAppointments = await repository.GetListAsync(a =>
                a.TrainerId == trainerId &&
                a.IsActive &&
                a.Status != AppointmentStatus.Cancelled &&
                (
                    // Yeni randevu, mevcut randevunun içinde mi?
                    (appointmentDate >= a.AppointmentDate && appointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    // Yeni randevunun bitişi, mevcut randevunun içinde mi?
                    (appointmentEndTime > a.AppointmentDate && appointmentEndTime <= a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    // Yeni randevu, mevcut randevuyu kapsıyor mu?
                    (appointmentDate <= a.AppointmentDate && appointmentEndTime >= a.AppointmentDate.AddMinutes(a.DurationMinutes))
                )
            );

            if (existingAppointments.Any())
            {
                return _responseHelper.SetError<bool>(
                    false,
                    "Antrenörün seçilen saatte başka bir randevusu var.",
                    400,
                    "APPOINTMENT_001");
            }

            // Antrenörün müsaitlik saatlerini kontrol et
            var availabilityRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<TrainerAvailability>();
            var dayOfWeek = appointmentDate.DayOfWeek;
            var appointmentTime = appointmentDate.TimeOfDay;
            var appointmentEndTimeSpan = appointmentEndTime.TimeOfDay;

            var availability = await availabilityRepository.GetFirstOrDefaultAsync(a =>
                a.TrainerId == trainerId &&
                a.DayOfWeek == dayOfWeek &&
                a.IsActive &&
                a.StartTime <= appointmentTime &&
                a.EndTime >= appointmentEndTimeSpan
            );

            if (availability == null)
            {
                return _responseHelper.SetError<bool>(
                    false,
                    "Antrenör seçilen gün ve saatte müsait değil.",
                    400,
                    "APPOINTMENT_002");
            }

            return _responseHelper.SetSuccess(true, "Antrenör müsait");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör müsaitliği kontrol edilirken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Müsaitlik kontrolü başarısız",
                "APPOINTMENT_003",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<bool>(false, errorInfo);
        }
    }

    public async Task<ServiceResponse<bool>> CheckMemberAvailabilityAsync(int memberId, DateTime appointmentDate, int durationMinutes)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointmentEndTime = appointmentDate.AddMinutes(durationMinutes);

            // Üyenin o saatte başka randevusu var mı?
            var existingAppointments = await repository.GetListAsync(a =>
                a.MemberId == memberId &&
                a.IsActive &&
                a.Status != AppointmentStatus.Cancelled &&
                (
                    (appointmentDate >= a.AppointmentDate && appointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    (appointmentEndTime > a.AppointmentDate && appointmentEndTime <= a.AppointmentDate.AddMinutes(a.DurationMinutes)) ||
                    (appointmentDate <= a.AppointmentDate && appointmentEndTime >= a.AppointmentDate.AddMinutes(a.DurationMinutes))
                )
            );

            if (existingAppointments.Any())
            {
                return _responseHelper.SetError<bool>(
                    false,
                    "Bu saatte başka bir randevunuz var.",
                    400,
                    "APPOINTMENT_004");
            }

            return _responseHelper.SetSuccess(true, "Üye müsait");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye müsaitliği kontrol edilirken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Müsaitlik kontrolü başarısız",
                "APPOINTMENT_005",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<bool>(false, errorInfo);
        }
    }

    public async Task<ServiceResponse<Appointment>> BookAppointmentAsync(Appointment appointment)
    {
        try
        {
            // 1. Antrenör müsaitlik kontrolü
            var trainerAvailability = await CheckTrainerAvailabilityAsync(
                appointment.TrainerId,
                appointment.AppointmentDate,
                appointment.DurationMinutes);

            if (!trainerAvailability.IsSuccessful)
            {
                return _responseHelper.SetError<Appointment>(
                    null,
                    trainerAvailability.Error?.ErrorMessage ?? "Antrenör müsait değil",
                    400,
                    trainerAvailability.Error?.ErrorCode ?? "APPOINTMENT_006");
            }

            // 2. Üye müsaitlik kontrolü
            var memberAvailability = await CheckMemberAvailabilityAsync(
                appointment.MemberId,
                appointment.AppointmentDate,
                appointment.DurationMinutes);

            if (!memberAvailability.IsSuccessful)
            {
                return _responseHelper.SetError<Appointment>(
                    null,
                    memberAvailability.Error?.ErrorMessage ?? "Bu saatte başka randevunuz var",
                    400,
                    memberAvailability.Error?.ErrorCode ?? "APPOINTMENT_007");
            }

            // 3. Salon çalışma saatlerini kontrol et
            var serviceRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var service = await serviceRepository.GetByIdAsync(appointment.ServiceId);
            
            if (service == null)
            {
                return _responseHelper.SetError<Appointment>(
                    null,
                    "Hizmet bulunamadı",
                    404,
                    "APPOINTMENT_008");
            }

            var workingHoursRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<WorkingHours>();
            var dayOfWeek = appointment.AppointmentDate.DayOfWeek;
            var appointmentTime = appointment.AppointmentDate.TimeOfDay;
            var appointmentEndTime = appointment.AppointmentDate.AddMinutes(appointment.DurationMinutes).TimeOfDay;

            var workingHours = await workingHoursRepository.GetFirstOrDefaultAsync(w =>
                w.GymLocationId == service.GymLocationId &&
                w.DayOfWeek == dayOfWeek &&
                w.IsActive &&
                !w.IsClosed &&
                w.OpenTime <= appointmentTime &&
                w.CloseTime >= appointmentEndTime
            );

            if (workingHours == null)
            {
                return _responseHelper.SetError<Appointment>(
                    null,
                    "Salon seçilen gün ve saatte kapalı",
                    400,
                    "APPOINTMENT_009");
            }

            // 4. Randevuyu oluştur
            appointment.Status = AppointmentStatus.Pending;
            appointment.CreatedAt = DateTime.Now;
            appointment.IsActive = true;

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            await repository.AddAsync(appointment);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Randevu oluşturuldu. AppointmentId: {AppointmentId}", appointment.Id);
            return _responseHelper.SetSuccess(appointment, "Randevu başarıyla oluşturuldu. Onay bekliyor.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Randevu oluşturulamadı",
                "APPOINTMENT_010",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<Appointment>(null, errorInfo);
        }
    }

    public async Task<ServiceResponse<Appointment>> ConfirmAppointmentAsync(int appointmentId)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.GetByIdAsync(appointmentId);

            if (appointment == null)
            {
                return _responseHelper.SetError<Appointment>(
                    null,
                    "Randevu bulunamadı",
                    404,
                    "APPOINTMENT_011");
            }

            if (appointment.Status != AppointmentStatus.Pending)
            {
                return _responseHelper.SetError<Appointment>(
                    null,
                    "Sadece bekleyen randevular onaylanabilir",
                    400,
                    "APPOINTMENT_012");
            }

            appointment.Status = AppointmentStatus.Confirmed;
            appointment.UpdatedAt = DateTime.Now;

            await repository.UpdateAsync(appointment);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Randevu onaylandı. AppointmentId: {AppointmentId}", appointmentId);
            return _responseHelper.SetSuccess(appointment, "Randevu onaylandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu onaylanırken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Randevu onaylanamadı",
                "APPOINTMENT_013",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<Appointment>(null, errorInfo);
        }
    }

    public async Task<ServiceResponse<bool>> CancelAppointmentAsync(int appointmentId, string? reason)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointment = await repository.GetByIdAsync(appointmentId);

            if (appointment == null)
            {
                return _responseHelper.SetError<bool>(
                    false,
                    "Randevu bulunamadı",
                    404,
                    "APPOINTMENT_014");
            }

            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                return _responseHelper.SetError<bool>(
                    false,
                    "Randevu zaten iptal edilmiş",
                    400,
                    "APPOINTMENT_015");
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                return _responseHelper.SetError<bool>(
                    false,
                    "Tamamlanmış randevular iptal edilemez",
                    400,
                    "APPOINTMENT_016");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrEmpty(reason))
            {
                appointment.Notes = $"İptal Nedeni: {reason}";
            }

            await repository.UpdateAsync(appointment);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Randevu iptal edildi. AppointmentId: {AppointmentId}", appointmentId);
            return _responseHelper.SetSuccess(true, "Randevu iptal edildi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu iptal edilirken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Randevu iptal edilemedi",
                "APPOINTMENT_017",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<bool>(false, errorInfo);
        }
    }

    public async Task<ServiceResponse<IEnumerable<Appointment>>> GetMemberAppointmentsAsync(int memberId)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointments = await repository.GetListAsync(a =>
                a.MemberId == memberId &&
                a.IsActive
            );

            return _responseHelper.SetSuccess<IEnumerable<Appointment>>(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye randevıları getirilirken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Randevular getirilemedi",
                "APPOINTMENT_018",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<IEnumerable<Appointment>>(null, errorInfo);
        }
    }

    public async Task<ServiceResponse<IEnumerable<Appointment>>> GetTrainerAppointmentsAsync(int trainerId)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointments = await repository.GetListAsync(a =>
                a.TrainerId == trainerId &&
                a.IsActive
            );

            return _responseHelper.SetSuccess<IEnumerable<Appointment>>(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör randevuları getirilirken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Randevular getirilemedi",
                "APPOINTMENT_019",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<IEnumerable<Appointment>>(null, errorInfo);
        }
    }

    public async Task<ServiceResponse<IEnumerable<int>>> GetAvailableTrainersAsync(int serviceId, DateTime appointmentDate, int durationMinutes)
    {
        try
        {
            // Hizmete sahip antrenörleri bul
            var specialtyRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<TrainerSpecialty>();
            var specialties = await specialtyRepository.GetListAsync(ts =>
                ts.ServiceId == serviceId &&
                ts.IsActive
            );

            var trainerIds = specialties.Select(ts => ts.TrainerId).Distinct().ToList();
            var availableTrainerIds = new List<int>();

            // Her antrenör için müsaitlik kontrolü
            foreach (var trainerId in trainerIds)
            {
                var availability = await CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);
                if (availability.IsSuccessful && availability.Data == true)
                {
                    availableTrainerIds.Add(trainerId);
                }
            }

            return _responseHelper.SetSuccess<IEnumerable<int>>(availableTrainerIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uygun antrenörler getirilirken hata oluştu");
            var errorInfo = new ErrorInfo(
                "Uygun antrenörler getirilemedi",
                "APPOINTMENT_020",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<IEnumerable<int>>(null, errorInfo);
        }
    }
}
