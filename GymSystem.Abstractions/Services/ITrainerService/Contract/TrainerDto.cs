namespace GymSystem.Application.Abstractions.Services.ITrainerService.Contract;

/// <summary>
/// Trainer için DTO - Request ve Response için kullanılır
/// </summary>
public class TrainerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public int GymLocationId { get; set; }
    
    // Navigation Properties (Response için)
    public string? GymLocationName { get; set; }
    
    // Hizmet Uzmanlıkları
    public List<int> SelectedServiceIds { get; set; } = new();
    public List<TrainerServiceInfo> Services { get; set; } = new();
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Antrenör hizmet bilgisi
/// </summary>
public class TrainerServiceInfo
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}
