using GymSystem.Application.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Belirli bir uzmanlık alanındaki antrenörleri getir (LINQ)
    /// </summary>
    [HttpGet("trainers-by-specialty")]
    public async Task<IActionResult> GetTrainersBySpecialty([FromQuery] string specialty)
    {
        var response = await _reportService.GetTrainersBySpecialtyAsync(specialty);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
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
        if (!TimeSpan.TryParse(time, out var appointmentTime))
        {
            return BadRequest(new { error = "Geçersiz saat formatı. Örnek: 10:00" });
        }

        var appointmentDateTime = date.Date.Add(appointmentTime);

        // Service bilgisini alıp duration'ı belirle (bu basit validation API'de kalabilir)
        var response = await _reportService.GetAvailableTrainersWithDetailsAsync(serviceId, appointmentDateTime, 60);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Belirli bir üyenin tüm randevularını getir (LINQ)
    /// </summary>
    [HttpGet("member-appointments")]
    public async Task<IActionResult> GetMemberAppointments([FromQuery] int memberId)
    {
        var response = await _reportService.GetMemberAppointmentsWithDetailsAsync(memberId);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// En popüler hizmetleri getir (LINQ - randevu sayısına göre)
    /// </summary>
    [HttpGet("popular-services")]
    public async Task<IActionResult> GetPopularServices([FromQuery] int top = 5)
    {
        var response = await _reportService.GetPopularServicesAsync(top);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Aylık gelir raporu (LINQ)
    /// </summary>
    [HttpGet("monthly-revenue")]
    public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int month, [FromQuery] int year)
    {
        var response = await _reportService.GetMonthlyRevenueAsync(month, year);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Antrenör iş yükü raporu (LINQ)
    /// </summary>
    [HttpGet("trainer-workload")]
    public async Task<IActionResult> GetTrainerWorkload([FromQuery] int? trainerId = null)
    {
        var response = await _reportService.GetTrainerWorkloadAsync(trainerId);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }
}
