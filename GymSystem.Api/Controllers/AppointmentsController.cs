using GymSystem.Application.Abstractions.Services;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _appointmentService.GetAllAsync();
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var response = await _appointmentService.GetByIdAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Randevu oluştur (tüm kontroller ile)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        var response = await _appointmentService.BookAppointmentAsync(appointment);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Appointment appointment)
    {
        if (id != appointment.Id)
        {
            return BadRequest(new { ErrorMessage = "Appointment ID mismatch", ErrorCode = "VALIDATION_001" });
        }

        var response = await _appointmentService.UpdateAsync(id, appointment);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _appointmentService.DeleteAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Randevu onayla (Admin/Trainer için)
    /// </summary>
    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var response = await _appointmentService.ConfirmAppointmentAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Randevu iptal et
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] string? reason)
    {
        var response = await _appointmentService.CancelAppointmentAsync(id, reason);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(new { success = response.Data, message = response.Message });
    }

    /// <summary>
    /// Üyenin tüm randevularını getir
    /// </summary>
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetMemberAppointments(int memberId)
    {
        var response = await _appointmentService.GetMemberAppointmentsAsync(memberId);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Antrenörün tüm randevularını getir
    /// </summary>
    [HttpGet("trainer/{trainerId}")]
    public async Task<IActionResult> GetTrainerAppointments(int trainerId)
    {
        var response = await _appointmentService.GetTrainerAppointmentsAsync(trainerId);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    /// <summary>
    /// Antrenör müsaitlik kontrolü
    /// </summary>
    [HttpGet("check-availability")]
    public async Task<IActionResult> CheckAvailability(
        [FromQuery] int trainerId,
        [FromQuery] DateTime appointmentDate,
        [FromQuery] int durationMinutes)
    {
        var response = await _appointmentService.CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(new { available = response.Data, message = response.Message });
    }

    /// <summary>
    /// Belirli bir hizmet için uygun antrenörleri getir
    /// </summary>
    [HttpGet("available-trainers")]
    public async Task<IActionResult> GetAvailableTrainers(
        [FromQuery] int serviceId,
        [FromQuery] DateTime appointmentDate,
        [FromQuery] int durationMinutes)
    {
        var response = await _appointmentService.GetAvailableTrainersAsync(serviceId, appointmentDate, durationMinutes);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }
}