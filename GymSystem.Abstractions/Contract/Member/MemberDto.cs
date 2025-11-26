using GymSystem.Domain.Enums;

namespace GymSystem.Application.Abstractions.Contract.Member;

/// <summary>
/// Member için tek DTO - Request ve Response için kullanılır
/// </summary>
public class MemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; } = Gender.Male;
    public DateTime? MembershipStartDate { get; set; } // Nullable - sadece üyelik onayında atanır
    public DateTime? MembershipEndDate { get; set; }
    public int? CurrentGymLocationId { get; set; }
    
    // Navigation Properties (Response için)
    public string? CurrentGymLocationName { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
