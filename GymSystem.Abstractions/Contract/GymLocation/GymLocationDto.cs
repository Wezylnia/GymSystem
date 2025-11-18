namespace GymSystem.Application.Abstractions.Contract.GymLocation;

/// <summary>
/// GymLocation için tek DTO - Request ve Response için kullanılır
/// </summary>
public class GymLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    public int Capacity { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
