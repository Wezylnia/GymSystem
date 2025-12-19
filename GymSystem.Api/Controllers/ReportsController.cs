using GymSystem.Application.Abstractions.Services.IReportService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,GymOwner")]
public class ReportsController : ControllerBase {
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger) {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("trainers-by-specialty")]
    public async Task<IActionResult> GetTrainersBySpecialty([FromQuery] string specialty) {
        var response = await _reportService.GetTrainersBySpecialtyAsync(specialty);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("available-trainers")]
    public async Task<IActionResult> GetAvailableTrainers(
        [FromQuery] int serviceId,
        [FromQuery] DateTime date,
        [FromQuery] string time) {
        if (!TimeSpan.TryParse(time, out var appointmentTime))
            return BadRequest(new { error = "Geçersiz saat formatı. Örnek: 10:00" });

        var appointmentDateTime = date.Date.Add(appointmentTime);
        var response = await _reportService.GetAvailableTrainersWithDetailsAsync(serviceId, appointmentDateTime, 60);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("member-appointments")]
    public async Task<IActionResult> GetMemberAppointments([FromQuery] int memberId) {
        var response = await _reportService.GetMemberAppointmentsWithDetailsAsync(memberId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("popular-services")]
    public async Task<IActionResult> GetPopularServices([FromQuery] int top = 5) {
        var response = await _reportService.GetPopularServicesAsync(top);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("monthly-revenue")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int month, [FromQuery] int year) {
        var response = await _reportService.GetMonthlyRevenueAsync(month, year);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("gym-owner-dashboard")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetGymOwnerDashboardStats([FromQuery] int? gymLocationId = null) {
        var response = await _reportService.GetGymOwnerDashboardStatsAsync(gymLocationId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("membership-statistics")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetMembershipStatistics([FromQuery] int? gymLocationId = null) {
        var response = await _reportService.GetMembershipStatisticsAsync(gymLocationId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("revenue-by-gym")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRevenueByGymLocation() {
        var response = await _reportService.GetRevenueByGymLocationAsync();

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("revenue-trend")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetRevenueTrend([FromQuery] int? gymLocationId = null) {
        var response = await _reportService.GetRevenueTrendAsync(gymLocationId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    /// <summary>
    /// Üye artış trendi (aylık)
    /// </summary>
    [HttpGet("member-growth-trend")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetMemberGrowthTrend([FromQuery] int? gymLocationId = null) {
        var response = await _reportService.GetMemberGrowthTrendAsync(gymLocationId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("trainer-workload")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetTrainerWorkload([FromQuery] int? trainerId = null) {
        var response = await _reportService.GetTrainerWorkloadAsync(trainerId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }
}