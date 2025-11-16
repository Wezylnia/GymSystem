namespace GymSystem.Domain.Entities;

/// <summary>
/// Spor salonu çalışma saatleri
/// Hangi gün, hangi saatler arası açık
/// </summary>
public class WorkingHours : BaseEntity
{
    public int GymLocationId { get; set; }
    public DayOfWeek DayOfWeek { get; set; } // Pazartesi, Salı, vb.
    public TimeSpan OpenTime { get; set; } // Açılış saati
    public TimeSpan CloseTime { get; set; } // Kapanış saati
    public bool IsClosed { get; set; } = false; // O gün kapalı mı?

    // Navigation properties
    public GymLocation GymLocation { get; set; } = null!;
}
