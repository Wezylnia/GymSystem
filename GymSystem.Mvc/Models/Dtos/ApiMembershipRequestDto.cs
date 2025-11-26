using GymSystem.Domain.Entities;

namespace GymSystem.Mvc.Models.Dtos;

/// <summary>
/// API'den dönen MembershipRequest DTO
/// </summary>
public class ApiMembershipRequestDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int GymLocationId { get; set; }
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty; // API'den string olarak geliyor
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties - API'den flat data geliyor
    public string? MemberName { get; set; }
    public string? MemberEmail { get; set; }
    public string? GymLocationName { get; set; }
    public string? GymLocationAddress { get; set; }
    
    // Helper property for enum conversion
    public MembershipRequestStatus StatusEnum => Enum.TryParse<MembershipRequestStatus>(Status, out var result) 
        ? result 
        : MembershipRequestStatus.Pending;
}
