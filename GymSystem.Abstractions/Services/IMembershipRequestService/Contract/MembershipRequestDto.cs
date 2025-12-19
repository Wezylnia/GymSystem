using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services.IMembershipRequestService.Contract;

/// <summary>
/// MembershipRequest için DTO
/// </summary>
public class MembershipRequestDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int GymLocationId { get; set; }
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    
    // Navigation Properties (Flat)
    public string? MemberName { get; set; }
    public string? MemberEmail { get; set; }
    public string? GymLocationName { get; set; }
    public string? GymLocationAddress { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
