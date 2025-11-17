using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Services;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Reports;

public class ReportService : IReportService
{
    private readonly IAppointmentService _appointmentService;
    private readonly ITrainerService _trainerService;
    private readonly IServiceService _serviceService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IAppointmentService appointmentService,
        ITrainerService trainerService,
        IServiceService serviceService,
        ILogger<ReportService> logger)
    {
        _appointmentService = appointmentService;
        _trainerService = trainerService;
        _serviceService = serviceService;
        _logger = logger;
    }

    public async Task<ServiceResponse<object>> GetTrainersBySpecialtyAsync(string specialty)
    {
        try
        {
            // Tüm servisleri al
            var servicesResponse = await _serviceService.GetAllAsync();
            if (!servicesResponse.IsSuccessful)
            {
                return new ServiceResponse<object> 
                { 
                    IsSuccessful = false, 
                    Error = new ErrorInfo("Servisler alınamadı", "REPORT_001", null, 500) 
                };
            }

            // LINQ: Specialty'ye göre filtrele
            var matchingService = servicesResponse.Data?
                .Where(s => s.Name.ToLower().Contains(specialty.ToLower()))
                .FirstOrDefault();

            if (matchingService == null)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = true,
                    Data = new { trainers = new List<object>(), message = $"'{specialty}' uzmanlığında hizmet bulunamadı" }
                };
            }

            // Tüm antrenörleri al
            var trainersResponse = await _trainerService.GetAllAsync();
            if (!trainersResponse.IsSuccessful)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = false,
                    Error = new ErrorInfo("Antrenörler alınamadı", "REPORT_002", null, 500)
                };
            }

            // LINQ: İlgili salonun antrenörlerini filtrele
            var trainers = trainersResponse.Data?
                .Where(t => t.GymLocationId == matchingService.GymLocationId && t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    FullName = $"{t.FirstName} {t.LastName}",
                    t.Email,
                    t.PhoneNumber,
                    t.Bio,
                    Specialty = specialty
                })
                .ToList();

            return new ServiceResponse<object>
            {
                IsSuccessful = true,
                Data = new { trainers, count = trainers?.Count ?? 0 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenörler specialty'ye göre getirilirken hata oluştu");
            return new ServiceResponse<object>
            {
                IsSuccessful = false,
                Error = new ErrorInfo(ex.Message, "REPORT_003", ex.StackTrace, 500)
            };
        }
    }

    public async Task<ServiceResponse<object>> GetAvailableTrainersWithDetailsAsync(int serviceId, DateTime appointmentDateTime, int durationMinutes)
    {
        try
        {
            // Uygun antrenörleri getir
            var availableTrainersResponse = await _appointmentService.GetAvailableTrainersAsync(
                serviceId,
                appointmentDateTime,
                durationMinutes);

            if (!availableTrainersResponse.IsSuccessful)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = false,
                    Error = new ErrorInfo("Uygun antrenörler alınamadı", "REPORT_004", null, 500)
                };
            }

            // Trainer detaylarını getir
            var trainersResponse = await _trainerService.GetAllAsync();
            if (!trainersResponse.IsSuccessful || trainersResponse.Data == null)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = true,
                    Data = new { trainers = new List<object>(), count = 0 }
                };
            }

            // LINQ: Uygun antrenörlerin detaylarını getir
            var trainerDetails = trainersResponse.Data
                .Where(t => availableTrainersResponse.Data!.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    FullName = $"{t.FirstName} {t.LastName}",
                    t.Email,
                    t.PhoneNumber,
                    AvailableDate = appointmentDateTime.ToString("dd.MM.yyyy"),
                    AvailableTime = appointmentDateTime.ToString("HH:mm")
                })
                .ToList();

            return new ServiceResponse<object>
            {
                IsSuccessful = true,
                Data = new { trainers = trainerDetails, count = trainerDetails.Count }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uygun antrenörler getirilirken hata oluştu");
            return new ServiceResponse<object>
            {
                IsSuccessful = false,
                Error = new ErrorInfo(ex.Message, "REPORT_005", ex.StackTrace, 500)
            };
        }
    }

    public async Task<ServiceResponse<object>> GetMemberAppointmentsWithDetailsAsync(int memberId)
    {
        try
        {
            var response = await _appointmentService.GetMemberAppointmentsAsync(memberId);

            if (!response.IsSuccessful)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = false,
                    Error = new ErrorInfo("Randevular alınamadı", "REPORT_006", null, 500)
                };
            }

            // LINQ: Randevuları tarihine göre sırala ve gerekli bilgileri seç
            var appointments = response.Data?
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => new
                {
                    a.Id,
                    Date = a.AppointmentDate.ToString("dd.MM.yyyy HH:mm"),
                    a.DurationMinutes,
                    a.Price,
                    Status = a.Status.ToString(),
                    a.Notes
                })
                .ToList();

            return new ServiceResponse<object>
            {
                IsSuccessful = true,
                Data = new { appointments, count = appointments?.Count ?? 0, memberId }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye randevuları getirilirken hata oluştu");
            return new ServiceResponse<object>
            {
                IsSuccessful = false,
                Error = new ErrorInfo(ex.Message, "REPORT_007", ex.StackTrace, 500)
            };
        }
    }

    public async Task<ServiceResponse<object>> GetPopularServicesAsync(int top = 5)
    {
        try
        {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var servicesResponse = await _serviceService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !servicesResponse.IsSuccessful)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = false,
                    Error = new ErrorInfo("Veriler alınamadı", "REPORT_008", null, 500)
                };
            }

            // LINQ: Hizmetleri randevu sayısına göre sırala
            var popularServices = appointmentsResponse.Data?
                .Where(a => a.IsActive)
                .GroupBy(a => a.ServiceId)
                .Select(g => new
                {
                    ServiceId = g.Key,
                    AppointmentCount = g.Count(),
                    TotalRevenue = g.Sum(a => a.Price)
                })
                .OrderByDescending(s => s.AppointmentCount)
                .Take(top)
                .Join(
                    servicesResponse.Data ?? Enumerable.Empty<Service>(),
                    stat => stat.ServiceId,
                    service => service.Id,
                    (stat, service) => new
                    {
                        ServiceId = service.Id,
                        ServiceName = service.Name,
                        stat.AppointmentCount,
                        stat.TotalRevenue,
                        AveragePrice = service.Price
                    }
                )
                .ToList();

            return new ServiceResponse<object>
            {
                IsSuccessful = true,
                Data = new { services = popularServices, count = popularServices?.Count ?? 0 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Popüler hizmetler getirilirken hata oluştu");
            return new ServiceResponse<object>
            {
                IsSuccessful = false,
                Error = new ErrorInfo(ex.Message, "REPORT_009", ex.StackTrace, 500)
            };
        }
    }

    public async Task<ServiceResponse<object>> GetMonthlyRevenueAsync(int month, int year)
    {
        try
        {
            var response = await _appointmentService.GetAllAsync();

            if (!response.IsSuccessful)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = false,
                    Error = new ErrorInfo("Randevular alınamadı", "REPORT_010", null, 500)
                };
            }

            // LINQ: Belirli ay ve yıldaki onaylanmış randevuları filtrele ve topla
            var monthlyData = response.Data?
                .Where(a =>
                    a.AppointmentDate.Year == year &&
                    a.AppointmentDate.Month == month &&
                    a.Status == AppointmentStatus.Confirmed &&
                    a.IsActive)
                .GroupBy(a => 1)
                .Select(g => new
                {
                    Month = $"{month}/{year}",
                    TotalAppointments = g.Count(),
                    TotalRevenue = g.Sum(a => a.Price),
                    AveragePrice = g.Average(a => a.Price),
                    MinPrice = g.Min(a => a.Price),
                    MaxPrice = g.Max(a => a.Price)
                })
                .FirstOrDefault();

            if (monthlyData == null)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = true,
                    Data = new
                    {
                        Month = $"{month}/{year}",
                        TotalAppointments = 0,
                        TotalRevenue = 0,
                        AveragePrice = 0,
                        MinPrice = 0,
                        MaxPrice = 0
                    }
                };
            }

            return new ServiceResponse<object>
            {
                IsSuccessful = true,
                Data = monthlyData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aylık gelir raporu getirilirken hata oluştu");
            return new ServiceResponse<object>
            {
                IsSuccessful = false,
                Error = new ErrorInfo(ex.Message, "REPORT_011", ex.StackTrace, 500)
            };
        }
    }

    public async Task<ServiceResponse<object>> GetTrainerWorkloadAsync(int? trainerId = null)
    {
        try
        {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var trainersResponse = await _trainerService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !trainersResponse.IsSuccessful)
            {
                return new ServiceResponse<object>
                {
                    IsSuccessful = false,
                    Error = new ErrorInfo("Veriler alınamadı", "REPORT_012", null, 500)
                };
            }

            // LINQ: Antrenör başına randevu istatistikleri
            var workloadQuery = appointmentsResponse.Data?
                .Where(a => a.IsActive && (!trainerId.HasValue || a.TrainerId == trainerId.Value))
                .GroupBy(a => a.TrainerId)
                .Select(g => new
                {
                    TrainerId = g.Key,
                    TotalAppointments = g.Count(),
                    PendingAppointments = g.Count(a => a.Status == AppointmentStatus.Pending),
                    ConfirmedAppointments = g.Count(a => a.Status == AppointmentStatus.Confirmed),
                    CompletedAppointments = g.Count(a => a.Status == AppointmentStatus.Completed),
                    TotalHours = g.Sum(a => a.DurationMinutes) / 60.0,
                    TotalRevenue = g.Sum(a => a.Price)
                })
                .Join(
                    trainersResponse.Data ?? Enumerable.Empty<Trainer>(),
                    stat => stat.TrainerId,
                    trainer => trainer.Id,
                    (stat, trainer) => new
                    {
                        stat.TrainerId,
                        TrainerName = $"{trainer.FirstName} {trainer.LastName}",
                        stat.TotalAppointments,
                        stat.PendingAppointments,
                        stat.ConfirmedAppointments,
                        stat.CompletedAppointments,
                        stat.TotalHours,
                        stat.TotalRevenue
                    }
                )
                .OrderByDescending(s => s.TotalAppointments)
                .ToList();

            return new ServiceResponse<object>
            {
                IsSuccessful = true,
                Data = new { workload = workloadQuery, count = workloadQuery?.Count ?? 0 }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör iş yükü raporu getirilirken hata oluştu");
            return new ServiceResponse<object>
            {
                IsSuccessful = false,
                Error = new ErrorInfo(ex.Message, "REPORT_013", ex.StackTrace, 500)
            };
        }
    }
}
