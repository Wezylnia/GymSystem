using GymSystem.Application.Abstractions.Contract.Appointment;
using GymSystem.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetAll()
    {
        var response = await _appointmentService.GetAllAsync();
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,GymOwner,Member")]
    public async Task<IActionResult> Get(int id)
    {
        var response = await _appointmentService.GetByIdAsync(id);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        if (response.Data == null)
            return NotFound(new { error = "Randevu bulunamadı" });

        return Ok(response.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create([FromBody] AppointmentDto request)
    {
        if (request == null)
            return BadRequest(new { error = "Geçersiz istek" });

        var response = await _appointmentService.BookAppointmentAsync(request);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage ?? "Randevu oluşturulamadı", errorCode = response.Error?.ErrorCode });

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Update(int id, AppointmentDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { ErrorMessage = "Appointment ID mismatch", ErrorCode = "VALIDATION_001" });

        var response = await _appointmentService.UpdateAsync(id, dto);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Member")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _appointmentService.DeleteAsync(id);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }

    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Confirm(int id)
    {
        var response = await _appointmentService.ConfirmAppointmentAsync(id);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(new { id = response.Data?.Id, status = response.Data?.Status, message = "Randevu onaylandı" });
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> Cancel(int id, [FromBody] string? reason)
    {
        var response = await _appointmentService.CancelAppointmentAsync(id, reason);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(new { success = response.Data, message = response.Message });
    }

    [HttpGet("member/{memberId}")]
    [Authorize(Roles = "Member,Admin,GymOwner")]
    public async Task<IActionResult> GetMemberAppointments(int memberId)
    {
        var response = await _appointmentService.GetMemberAppointmentsAsync(memberId);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("trainer/{trainerId}")]
    [Authorize(Roles = "Admin,GymOwner,Trainer")]
    public async Task<IActionResult> GetTrainerAppointments(int trainerId)
    {
        var response = await _appointmentService.GetTrainerAppointmentsAsync(trainerId);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("check-availability")]
    [Authorize(Roles = "Member,Admin,GymOwner")]
    public async Task<IActionResult> CheckAvailability([FromQuery] int trainerId, [FromQuery] DateTime appointmentDate, [FromQuery] int durationMinutes)
    {
        var response = await _appointmentService.CheckTrainerAvailabilityAsync(trainerId, appointmentDate, durationMinutes);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(new { available = response.Data, message = response.Message });
    }

    [HttpGet("available-trainers")]
    [Authorize(Roles = "Member,Admin,GymOwner")]
    public async Task<IActionResult> GetAvailableTrainers([FromQuery] int serviceId, [FromQuery] DateTime appointmentDate, [FromQuery] int durationMinutes)
    {
        var response = await _appointmentService.GetAvailableTrainersAsync(serviceId, appointmentDate, durationMinutes);
        
        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }
}