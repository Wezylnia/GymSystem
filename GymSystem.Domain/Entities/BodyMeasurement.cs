namespace GymSystem.Domain.Entities;

/// <summary>
/// Üyelerin boy ve kilo takibi için ölçüm kayýtlarý
/// </summary>
public class BodyMeasurement : BaseEntity {
    public int MemberId { get; set; }

    /// <summary>
    /// Ölçüm tarihi
    /// </summary>
    public DateTime MeasurementDate { get; set; }

    /// <summary>
    /// Boy (cm)
    /// </summary>
    public decimal Height { get; set; }

    /// <summary>
    /// Kilo (kg)
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Kullanýcý notu (opsiyonel)
    /// </summary>
    public string? Note { get; set; }

    // Navigation property
    public Member Member { get; set; } = null!;
}
