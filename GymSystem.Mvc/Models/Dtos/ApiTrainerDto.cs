namespace GymSystem.Mvc.Models.Dtos;

/// <summary>
/// API'den dönen Trainer DTO
/// </summary>
public class ApiTrainerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public int GymLocationId { get; set; }
    public string? GymLocationName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
