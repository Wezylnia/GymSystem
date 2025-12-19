namespace GymSystem.Domain.Entities;

/// <summary>
/// Antrenör/Eğitmen
/// </summary>
public class Trainer : BaseEntity {
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; } // Kısa özgeçmiş
    public string? PhotoUrl { get; set; }
    public int GymLocationId { get; set; }

    // Navigation properties
    public GymLocation GymLocation { get; set; } = null!;
    public ICollection<TrainerSpecialty> Specialties { get; set; } = new List<TrainerSpecialty>();
    public ICollection<TrainerAvailability> Availabilities { get; set; } = new List<TrainerAvailability>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
