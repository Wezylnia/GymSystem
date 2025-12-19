namespace GymSystem.Domain.Entities;

/// <summary>
/// Antrenör uzmanlık alanları (Many-to-Many: Trainer <-> Service)
/// Bir antrenör birden fazla hizmette uzman olabilir
/// </summary>
public class TrainerSpecialty : BaseEntity {
    public int TrainerId { get; set; }
    public int ServiceId { get; set; }
    public int ExperienceYears { get; set; } // Kaç yıl deneyimi var
    public string? CertificateName { get; set; } // Sertifika adı

    // Navigation properties
    public Trainer Trainer { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
