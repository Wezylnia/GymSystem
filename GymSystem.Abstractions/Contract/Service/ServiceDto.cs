namespace GymSystem.Application.Abstractions.Contract.Service;

/// <summary>
/// Service için DTO - Request ve Response için kullanılır
/// </summary>
public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int GymLocationId { get; set; }
    
    // Navigation Properties (Response için)
    public string? GymLocationName { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
