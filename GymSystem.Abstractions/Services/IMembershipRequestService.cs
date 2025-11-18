using GymSystem.Application.Abstractions.Contract.MembershipRequest;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

/// <summary>
/// Membership request service interface - DTO + ServiceResponse pattern
/// </summary>
public interface IMembershipRequestService : IApplicationService
{
    Task<ServiceResponse<MembershipRequestDto>> CreateRequestAsync(int memberId, int gymLocationId, MembershipDuration duration, decimal price, string? notes = null);
    Task<ServiceResponse<List<MembershipRequestDto>>> GetMemberRequestsAsync(int memberId);
    Task<ServiceResponse<List<MembershipRequestDto>>> GetGymLocationRequestsAsync(int gymLocationId);
    Task<ServiceResponse<MembershipRequestDto?>> GetRequestByIdAsync(int id);
    Task<ServiceResponse<List<MembershipRequestDto>>> GetAllRequestsAsync();
    Task<ServiceResponse<List<MembershipRequestDto>>> GetPendingRequestsAsync();
    Task<ServiceResponse<bool>> ApproveRequestAsync(int id, int approvedByUserId, string? adminNotes = null);
    Task<ServiceResponse<bool>> RejectRequestAsync(int id, int rejectedByUserId, string? adminNotes = null);
    Task<ServiceResponse<bool>> DeleteRequestAsync(int id);
}
