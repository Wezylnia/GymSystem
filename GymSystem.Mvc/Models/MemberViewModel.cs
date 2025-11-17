namespace GymSystem.Mvc.Models;

public class MemberViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
    public int? CurrentGymLocationId { get; set; }
    public string? CurrentGymLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Helper properties
    public bool HasActiveMembership => MembershipEndDate.HasValue && MembershipEndDate.Value > DateTime.Now;
    public int? DaysRemaining => HasActiveMembership 
        ? (MembershipEndDate!.Value - DateTime.Now).Days 
        : null;
    public string MembershipStatus => HasActiveMembership 
        ? "Aktif Üyelik" 
        : (MembershipEndDate.HasValue ? "Süresi Dolmuş" : "Üyelik Yok");
}

public class CreateMemberViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime MembershipStartDate { get; set; } = DateTime.Now;
    public DateTime? MembershipEndDate { get; set; }
}
