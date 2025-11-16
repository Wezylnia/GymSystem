namespace GymSystem.Domain.Entities;

/// <summary>
/// Spor Salonu (Fitness Center)
/// </summary>
public class GymLocation : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
    public ICollection<Trainer> Trainers { get; set; } = new List<Trainer>();
}
