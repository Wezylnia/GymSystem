using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface IMembershipRequestService
{
    /// <summary>
    /// Yeni üyelik talebi oluşturur
    /// </summary>
    Task<MembershipRequest> CreateRequestAsync(int memberId, int gymLocationId, MembershipDuration duration, 
        decimal price, string? notes = null);

    /// <summary>
    /// Belirli bir üyenin tüm taleplerini getirir
    /// </summary>
    Task<List<MembershipRequest>> GetMemberRequestsAsync(int memberId);

    /// <summary>
    /// Belirli bir salonun tüm taleplerini getirir
    /// </summary>
    Task<List<MembershipRequest>> GetGymLocationRequestsAsync(int gymLocationId);

    /// <summary>
    /// ID'ye göre talebi getirir
    /// </summary>
    Task<MembershipRequest?> GetRequestByIdAsync(int id);

    /// <summary>
    /// Tüm talepleri getirir (Admin için)
    /// </summary>
    Task<List<MembershipRequest>> GetAllRequestsAsync();

    /// <summary>
    /// Bekleyen talepleri getirir
    /// </summary>
    Task<List<MembershipRequest>> GetPendingRequestsAsync();

    /// <summary>
    /// Talebi onaylar
    /// </summary>
    Task<bool> ApproveRequestAsync(int id, int approvedByUserId, string? adminNotes = null);

    /// <summary>
    /// Talebi reddeder
    /// </summary>
    Task<bool> RejectRequestAsync(int id, int rejectedByUserId, string? adminNotes = null);

    /// <summary>
    /// Talebi siler (Sadece Pending durumunda)
    /// </summary>
    Task<bool> DeleteRequestAsync(int id);
}
