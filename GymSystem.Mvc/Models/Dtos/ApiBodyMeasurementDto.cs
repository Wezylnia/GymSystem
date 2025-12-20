namespace GymSystem.Mvc.Models.Dtos;

/// <summary>
/// API'den gelen Body Measurement verisi için DTO
/// </summary>
public class ApiBodyMeasurementDto {
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? MemberName { get; set; }
    public DateTime MeasurementDate { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string? Note { get; set; }
    public decimal? HeightChange { get; set; }
    public decimal? WeightChange { get; set; }
    public decimal BMI { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
