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
    public MembershipRequestStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ApiMemberDto? Member { get; set; }
    public ApiGymLocationDto? GymLocation { get; set; }
}
