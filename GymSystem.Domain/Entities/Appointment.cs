namespace GymSystem.Domain.Entities;

/// <summary>
/// Randevu sistemi
/// Üye, antrenör ve hizmet bilgisi
/// </summary>
public class Appointment : BaseEntity {
    public int MemberId { get; set; }
    public int TrainerId { get; set; }
    public int ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; } // Randevu tarihi ve saati
    public int DurationMinutes { get; set; } // Randevu süresi
    public decimal Price { get; set; } // Ücret
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; } // Özel notlar

    // Navigation properties
    public Member Member { get; set; } = null!;
    public Trainer Trainer { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

/// <summary>
/// Randevu durumları
/// </summary>
public enum AppointmentStatus {
    Pending = 0,    // Onay bekliyor
    Confirmed = 1,  // Onaylandı
    Cancelled = 2,  // İptal edildi
    Completed = 3   // Tamamlandı
}
