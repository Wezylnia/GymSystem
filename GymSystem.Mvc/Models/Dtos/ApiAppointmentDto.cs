namespace GymSystem.Mvc.Models.Dtos;

/// <summary>
/// API'den dönen Appointment DTO
/// </summary>
public class ApiAppointmentDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int TrainerId { get; set; }
    public int ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public ApiMemberDto? Member { get; set; }
    public ApiTrainerDto? Trainer { get; set; }
    public ApiServiceDto? Service { get; set; }
}
