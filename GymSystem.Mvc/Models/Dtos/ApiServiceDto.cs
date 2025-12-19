namespace GymSystem.Mvc.Models.Dtos;

/// <summary>
/// API'den dönen Service DTO
/// </summary>
public class ApiServiceDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int GymLocationId { get; set; }
    public string? GymLocationName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
