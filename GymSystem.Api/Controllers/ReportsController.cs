using GymSystem.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ITrainerService _trainerService;
    private readonly IServiceService _serviceService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IAppointmentService appointmentService,
        ITrainerService trainerService,
        IServiceService serviceService,
        ILogger<ReportsController> logger)
    {
        _appointmentService = appointmentService;
        _trainerService = trainerService;
        _serviceService = serviceService;
        _logger = logger;
    }

    /// <summary>
    /// Belirli bir uzmanlık alanındaki antrenörleri getir (LINQ)
    /// </summary>
    [HttpGet("trainers-by-specialty")]
    public async Task<IActionResult> GetTrainersBySpecialty([FromQuery] string specialty)
    {
        try
        {
            // Tüm servisleri al
            var servicesResponse = await _serviceService.GetAllAsync();
            if (!servicesResponse.IsSuccessful)
            {
                return StatusCode(500, new { error = "Servisler alınamadı" });
            }

            // LINQ: Specialty'ye göre filtrele
            var matchingService = servicesResponse.Data?
                .Where(s => s.Name.ToLower().Contains(specialty.ToLower()))
                .FirstOrDefault();

            if (matchingService == null)
            {
                return Ok(new { trainers = new List<object>(), message = $"'{specialty}' uzmanlığında hizmet bulunamadı" });
            }

            // Tüm antrenörleri al
            var trainersResponse = await _trainerService.GetAllAsync();
            if (!trainersResponse.IsSuccessful)
            {
                return StatusCode(500, new { error = "Antrenörler alınamadı" });
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

            return Ok(new { trainers, count = trainers?.Count ?? 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenörler specialty'ye göre getirilirken hata oluştu");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir tarih ve saatte uygun antrenörleri getir (LINQ)
    /// </summary>
    [HttpGet("available-trainers")]
    public async Task<IActionResult> GetAvailableTrainers(
        [FromQuery] int serviceId,
        [FromQuery] DateTime date,
        [FromQuery] string time)
    {
        try
        {
            if (!TimeSpan.TryParse(time, out var appointmentTime))
            {
                return BadRequest(new { error = "Geçersiz saat formatı. Örnek: 10:00" });
            }

            var appointmentDateTime = date.Date.Add(appointmentTime);
            
            // Service bilgisini al
            var serviceResponse = await _serviceService.GetByIdAsync(serviceId);
            if (!serviceResponse.IsSuccessful)
            {
                return NotFound(new { error = "Hizmet bulunamadı" });
            }

            var durationMinutes = serviceResponse.Data?.DurationMinutes ?? 60;

            // Uygun antrenörleri getir
            var availableTrainersResponse = await _appointmentService.GetAvailableTrainersAsync(
                serviceId, 
                appointmentDateTime, 
                durationMinutes);

            if (!availableTrainersResponse.IsSuccessful)
            {
                return StatusCode(500, new { error = "Uygun antrenörler alınamadı" });
            }

            // Trainer detaylarını getir
            var trainersResponse = await _trainerService.GetAllAsync();
            if (trainersResponse.IsSuccessful && trainersResponse.Data != null)
            {
                // LINQ: Uygun antrenörlerin detaylarını getir
                var trainerDetails = trainersResponse.Data
                    .Where(t => availableTrainersResponse.Data!.Contains(t.Id))
                    .Select(t => new
                    {
                        t.Id,
                        FullName = $"{t.FirstName} {t.LastName}",
                        t.Email,
                        t.PhoneNumber,
                        AvailableDate = date.ToString("dd.MM.yyyy"),
                        AvailableTime = time
                    })
                    .ToList();

                return Ok(new { trainers = trainerDetails, count = trainerDetails.Count });
            }

            return Ok(new { trainers = new List<object>(), count = 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uygun antrenörler getirilirken hata oluştu");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Belirli bir üyenin tüm randevularını getir (LINQ)
    /// </summary>
    [HttpGet("member-appointments")]
    public async Task<IActionResult> GetMemberAppointments([FromQuery] int memberId)
    {
        try
        {
            var response = await _appointmentService.GetMemberAppointmentsAsync(memberId);
            
            if (!response.IsSuccessful)
            {
                return StatusCode(500, new { error = "Randevular alınamadı" });
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

            return Ok(new { appointments, count = appointments?.Count ?? 0, memberId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye randevuları getirilirken hata oluştu");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// En popüler hizmetleri getir (LINQ - randevu sayısına göre)
    /// </summary>
    [HttpGet("popular-services")]
    public async Task<IActionResult> GetPopularServices([FromQuery] int top = 5)
    {
        try
        {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var servicesResponse = await _serviceService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !servicesResponse.IsSuccessful)
            {
                return StatusCode(500, new { error = "Veriler alınamadı" });
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
                    servicesResponse.Data ?? Enumerable.Empty<Domain.Entities.Service>(),
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

            return Ok(new { services = popularServices, count = popularServices?.Count ?? 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Popüler hizmetler getirilirken hata oluştu");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aylık gelir raporu (LINQ)
    /// </summary>
    [HttpGet("monthly-revenue")]
    public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int month, [FromQuery] int year)
    {
        try
        {
            var response = await _appointmentService.GetAllAsync();
            
            if (!response.IsSuccessful)
            {
                return StatusCode(500, new { error = "Randevular alınamadı" });
            }

            // LINQ: Belirli ay ve yıldaki onaylanmış randevuları filtrele ve topla
            var monthlyData = response.Data?
                .Where(a => 
                    a.AppointmentDate.Year == year && 
                    a.AppointmentDate.Month == month &&
                    a.Status == Domain.Entities.AppointmentStatus.Confirmed &&
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
                return Ok(new 
                { 
                    Month = $"{month}/{year}",
                    TotalAppointments = 0,
                    TotalRevenue = 0,
                    AveragePrice = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                });
            }

            return Ok(monthlyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aylık gelir raporu getirilirken hata oluştu");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Antrenör iş yükü raporu (LINQ)
    /// </summary>
    [HttpGet("trainer-workload")]
    public async Task<IActionResult> GetTrainerWorkload([FromQuery] int? trainerId = null)
    {
        try
        {
            var appointmentsResponse = await _appointmentService.GetAllAsync();
            var trainersResponse = await _trainerService.GetAllAsync();

            if (!appointmentsResponse.IsSuccessful || !trainersResponse.IsSuccessful)
            {
                return StatusCode(500, new { error = "Veriler alınamadı" });
            }

            // LINQ: Antrenör başına randevu istatistikleri
            var workloadQuery = appointmentsResponse.Data?
                .Where(a => a.IsActive && (!trainerId.HasValue || a.TrainerId == trainerId.Value))
                .GroupBy(a => a.TrainerId)
                .Select(g => new
                {
                    TrainerId = g.Key,
                    TotalAppointments = g.Count(),
                    PendingAppointments = g.Count(a => a.Status == Domain.Entities.AppointmentStatus.Pending),
                    ConfirmedAppointments = g.Count(a => a.Status == Domain.Entities.AppointmentStatus.Confirmed),
                    CompletedAppointments = g.Count(a => a.Status == Domain.Entities.AppointmentStatus.Completed),
                    TotalHours = g.Sum(a => a.DurationMinutes) / 60.0,
                    TotalRevenue = g.Sum(a => a.Price)
                })
                .Join(
                    trainersResponse.Data ?? Enumerable.Empty<Domain.Entities.Trainer>(),
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

            return Ok(new { workload = workloadQuery, count = workloadQuery?.Count ?? 0 });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Antrenör iş yükü raporu getirilirken hata oluştu");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
