namespace GymSystem.Domain.Entities;

/// <summary>
/// Spor salonu hizmetleri (Fitness, Yoga, Pilates, Cardio, Zumba vb.)
/// </summary>
public class Service : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } // Hizmet süresi (dakika)
    public decimal Price { get; set; } // Ücret
    public int GymLocationId { get; set; }

    // Navigation properties
    public GymLocation GymLocation { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<TrainerSpecialty> TrainerSpecialties { get; set; } = new List<TrainerSpecialty>();
}
