namespace GymSystem.Domain.Entities;

/// <summary>
/// Antrenör müsaitlik saatleri
/// Hangi gün, hangi saatte antrenör müsait
/// </summary>
public class TrainerAvailability : BaseEntity
{
    public int TrainerId { get; set; }
    public DayOfWeek DayOfWeek { get; set; } // Pazartesi, Salı, vb.
    public TimeSpan StartTime { get; set; } // Başlangıç saati (örn: 09:00)
    public TimeSpan EndTime { get; set; } // Bitiş saati (örn: 17:00)

    // Navigation properties
    public Trainer Trainer { get; set; } = null!;
}
