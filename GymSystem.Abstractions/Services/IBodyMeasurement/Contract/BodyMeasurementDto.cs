namespace GymSystem.Application.Abstractions.Services.IBodyMeasurement.Contract;

/// <summary>
/// Boy-Kilo ölçüm kaydý DTO
/// </summary>
public class BodyMeasurementDto {
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? MemberName { get; set; }
    
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
    /// Kullanýcý notu
    /// </summary>
    public string? Note { get; set; }
    
    /// <summary>
    /// Bir önceki ölçüme göre boy deðiþimi (cm)
    /// </summary>
    public decimal? HeightChange { get; set; }
    
    /// <summary>
    /// Bir önceki ölçüme göre kilo deðiþimi (kg)
    /// </summary>
    public decimal? WeightChange { get; set; }
    
    /// <summary>
    /// BMI (Vücut Kitle Ýndeksi)
    /// </summary>
    public decimal BMI => Height > 0 ? Weight / ((Height / 100) * (Height / 100)) : 0;
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
