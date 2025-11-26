using GymSystem.Domain.Enums;

namespace GymSystem.Mvc.Models.Dtos;

public class ApiMemberDto {
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; }
    public DateTime? MembershipStartDate { get; set; } // Nullable
    public DateTime? MembershipEndDate { get; set; }
    public int? CurrentGymLocationId { get; set; }
    public string? CurrentGymLocationName { get; set; } // Flat field - API'den geliyor
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ApiGymLocationDto {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}