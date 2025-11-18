using GymSystem.Application.Abstractions.Services;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Tüm endpoint'ler için default authorization
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
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        // Entity'den DTO'ya dönüştür (Status enum'u string'e çevir)
        var dtos = response.Data?.Select(a => new
        {
            a.Id,
            a.MemberId,
            a.TrainerId,
            a.ServiceId,
            a.AppointmentDate,
            a.DurationMinutes,
            a.Price,
            Status = a.Status.ToString(), // Enum'u string'e çevir
            a.Notes,
            a.IsActive,
            a.CreatedAt,
            MemberName = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : null,
            TrainerName = a.Trainer != null ? $"{a.Trainer.FirstName} {a.Trainer.LastName}" : null,
            ServiceName = a.Service?.Name,
            GymLocationName = a.Service?.GymLocation?.Name
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,GymOwner,Member")]
    public async Task<IActionResult> Get(int id)
    {
        var response = await _appointmentService.GetByIdAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        if (response.Data == null)
        {
            return NotFound(new { error = "Randevu bulunamadı" });
        }

        // Entity'den DTO'ya dönüştür
        var dto = new
        {
            response.Data.Id,
            response.Data.MemberId,
            response.Data.TrainerId,
            response.Data.ServiceId,
            response.Data.AppointmentDate,
            response.Data.DurationMinutes,
            response.Data.Price,
            Status = response.Data.Status.ToString(), // Enum'u string'e çevir
            response.Data.Notes,
            response.Data.IsActive,
            response.Data.CreatedAt,
            MemberName = response.Data.Member != null ? $"{response.Data.Member.FirstName} {response.Data.Member.LastName}" : null,
            TrainerName = response.Data.Trainer != null ? $"{response.Data.Trainer.FirstName} {response.Data.Trainer.LastName}" : null,
            ServiceName = response.Data.Service?.Name,
            GymLocationName = response.Data.Service?.GymLocation?.Name
        };

        return Ok(dto);
    }

    /// <summary>
    /// Randevu oluştur (tüm kontroller ile)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "Geçersiz istek" });
        }

        // Entity oluştur (navigation property'ler olmadan)
        var appointment = new Appointment
        {
            MemberId = request.MemberId,
            TrainerId = request.TrainerId,
            ServiceId = request.ServiceId,
            AppointmentDate = request.AppointmentDate,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Notes = request.Notes
        };

        var response = await _appointmentService.BookAppointmentAsync(appointment);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, new { 
                error = response.Error?.ErrorMessage ?? "Randevu oluşturulamadı",
                errorCode = response.Error?.ErrorCode 
            });
        }

        // Entity'den DTO'ya dönüştür
        var dto = new
        {
            response.Data!.Id,
            response.Data.MemberId,
            response.Data.TrainerId,
            response.Data.ServiceId,
            response.Data.AppointmentDate,
            response.Data.DurationMinutes,
            response.Data.Price,
            response.Data.Status,
            response.Data.Notes,
            response.Data.CreatedAt
        };

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, dto);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
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
    [Authorize(Roles = "Admin,Member")]
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
    /// Randevu onayla (Sadece Admin ve GymOwner)
    /// </summary>
    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Confirm(int id)
    {
        var response = await _appointmentService.ConfirmAppointmentAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(new { 
            id = response.Data?.Id,
            status = response.Data?.Status.ToString(),
            message = "Randevu onaylandi" 
        });
    }

    /// <summary>
    /// Randevu iptal et
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Member,Admin")]
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
    [Authorize(Roles = "Member,Admin,GymOwner")]
    public async Task<IActionResult> GetMemberAppointments(int memberId)
    {
        var response = await _appointmentService.GetMemberAppointmentsAsync(memberId);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        // Entity'den DTO'ya dönüştür (Status enum'u string'e çevir)
        var dtos = response.Data?.Select(a => new
        {
            a.Id,
            a.MemberId,
            a.TrainerId,
            a.ServiceId,
            a.AppointmentDate,
            a.DurationMinutes,
            a.Price,
            Status = a.Status.ToString(),
            a.Notes,
            a.IsActive,
            a.CreatedAt,
            MemberName = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : null,
            TrainerName = a.Trainer != null ? $"{a.Trainer.FirstName} {a.Trainer.LastName}" : null,
            ServiceName = a.Service?.Name,
            GymLocationName = a.Service?.GymLocation?.Name
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Antrenörün tüm randevularını getir
    /// </summary>
    [HttpGet("trainer/{trainerId}")]
    [Authorize(Roles = "Admin,GymOwner,Trainer")]
    public async Task<IActionResult> GetTrainerAppointments(int trainerId)
    {
        var response = await _appointmentService.GetTrainerAppointmentsAsync(trainerId);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        // Entity'den DTO'ya dönüştür (Status enum'u string'e çevir)
        var dtos = response.Data?.Select(a => new
        {
            a.Id,
            a.MemberId,
            a.TrainerId,
            a.ServiceId,
            a.AppointmentDate,
            a.DurationMinutes,
            a.Price,
            Status = a.Status.ToString(),
            a.Notes,
            a.IsActive,
            a.CreatedAt,
            MemberName = a.Member != null ? $"{a.Member.FirstName} {a.Member.LastName}" : null,
            TrainerName = a.Trainer != null ? $"{a.Trainer.FirstName} {a.Trainer.LastName}" : null,
            ServiceName = a.Service?.Name,
            GymLocationName = a.Service?.GymLocation?.Name
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Antrenör müsaitlik kontrolü
    /// </summary>
    [HttpGet("check-availability")]
    [Authorize(Roles = "Member,Admin,GymOwner")]
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
    [Authorize(Roles = "Member,Admin,GymOwner")]
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

// Request DTOs
public record CreateAppointmentRequest(
    int MemberId,
    int TrainerId,
    int ServiceId,
    DateTime AppointmentDate,
    int DurationMinutes,
    decimal Price,
    string? Notes
);