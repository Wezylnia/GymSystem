namespace GymSystem.Application.Abstractions.Contract.Appointment;

public class AppointmentDto
{
    // Identity
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int TrainerId { get; set; }
    public int ServiceId { get; set; }
    
    // Appointment Details
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    
    // Navigation Properties (Response için)
    public string? MemberName { get; set; }
    public string? TrainerName { get; set; }
    public string? ServiceName { get; set; }
    public string? GymLocationName { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}