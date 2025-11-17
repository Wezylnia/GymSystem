using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using GymSystem.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Membership;

public class MembershipRequestService : IMembershipRequestService
{
    private readonly GymDbContext _context;
    private readonly ILogger<MembershipRequestService> _logger;

    public MembershipRequestService(
        GymDbContext context,
        ILogger<MembershipRequestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MembershipRequest> CreateRequestAsync(int memberId, int gymLocationId, 
        MembershipDuration duration, decimal price, string? notes = null)
    {
        try
        {
            // Member kontrolü
            var member = await _context.Set<Member>().FindAsync(memberId);
            if (member == null)
            {
                throw new ArgumentException($"Member ID {memberId} bulunamadı.");
            }

            // Aktif üyelik kontrolü
            if (member.HasActiveMembership())
            {
                var daysRemaining = member.DaysUntilMembershipExpires();
                throw new InvalidOperationException(
                    $"Zaten aktif bir üyeliğiniz bulunmaktadır. " +
                    $"Üyeliğiniz {member.MembershipEndDate:dd.MM.yyyy} tarihinde sona erecek " +
                    $"({daysRemaining} gün kaldı). " +
                    $"Mevcut üyeliğiniz bitmeden yeni talep oluşturamazsınız.");
            }

            // GymLocation kontrolü
            var gymLocation = await _context.Set<GymLocation>().FindAsync(gymLocationId);
            if (gymLocation == null)
            {
                throw new ArgumentException($"Gym Location ID {gymLocationId} bulunamadı.");
            }

            // Aynı salon için bekleyen talep var mı kontrol et
            var existingPendingRequest = await _context.Set<MembershipRequest>()
                .FirstOrDefaultAsync(mr => mr.MemberId == memberId 
                    && mr.GymLocationId == gymLocationId 
                    && mr.Status == MembershipRequestStatus.Pending
                    && mr.IsActive);

            if (existingPendingRequest != null)
            {
                throw new InvalidOperationException("Bu salon için zaten bekleyen bir talebiniz var.");
            }

            var request = new MembershipRequest
            {
                MemberId = memberId,
                GymLocationId = gymLocationId,
                Duration = duration,
                Price = price,
                Notes = notes,
                Status = MembershipRequestStatus.Pending,
                CreatedAt = DateTimeHelper.Now,
                IsActive = true
            };

            _context.Set<MembershipRequest>().Add(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi oluşturuldu. Member ID: {MemberId}, Gym ID: {GymId}, Request ID: {RequestId}", 
                memberId, gymLocationId, request.Id);

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üyelik talebi oluşturulurken hata. Member ID: {MemberId}", memberId);
            throw;
        }
    }

    public async Task<List<MembershipRequest>> GetMemberRequestsAsync(int memberId)
    {
        return await _context.Set<MembershipRequest>()
            .Where(mr => mr.MemberId == memberId && mr.IsActive)
            .Include(mr => mr.GymLocation)
            .Include(mr => mr.Member)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MembershipRequest>> GetGymLocationRequestsAsync(int gymLocationId)
    {
        return await _context.Set<MembershipRequest>()
            .Where(mr => mr.GymLocationId == gymLocationId && mr.IsActive)
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<MembershipRequest?> GetRequestByIdAsync(int id)
    {
        return await _context.Set<MembershipRequest>()
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .FirstOrDefaultAsync(mr => mr.Id == id && mr.IsActive);
    }

    public async Task<List<MembershipRequest>> GetAllRequestsAsync()
    {
        return await _context.Set<MembershipRequest>()
            .Where(mr => mr.IsActive)
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MembershipRequest>> GetPendingRequestsAsync()
    {
        return await _context.Set<MembershipRequest>()
            .Where(mr => mr.Status == MembershipRequestStatus.Pending && mr.IsActive)
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveRequestAsync(int id, int approvedByUserId, string? adminNotes = null)
    {
        try
        {
            var request = await _context.Set<MembershipRequest>().FindAsync(id);
            if (request == null || !request.IsActive)
            {
                return false;
            }

            if (request.Status != MembershipRequestStatus.Pending)
            {
                throw new InvalidOperationException("Sadece bekleyen talepler onaylanabilir.");
            }

            request.Status = MembershipRequestStatus.Approved;
            request.AdminNotes = adminNotes;
            request.ApprovedBy = approvedByUserId;
            request.ApprovedAt = DateTimeHelper.Now;
            request.UpdatedAt = DateTimeHelper.Now;

            // Member'ın üyelik tarihlerini ve salon bilgisini güncelle
            var member = await _context.Set<Member>().FindAsync(request.MemberId);
            if (member != null)
            {
                var now = DateTimeHelper.Now;
                var startDate = member.MembershipEndDate > now
                    ? member.MembershipEndDate.Value 
                    : now;
                    
                member.MembershipStartDate = startDate;
                member.MembershipEndDate = startDate.AddMonthsSafe((int)request.Duration);
                member.CurrentGymLocationId = request.GymLocationId;
                member.IsActive = true;
                member.UpdatedAt = DateTimeHelper.Now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi onaylandı. Request ID: {RequestId}, Approved By: {UserId}, Gym: {GymId}", 
                id, approvedByUserId, request.GymLocationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep onaylanırken hata. Request ID: {RequestId}", id);
            throw;
        }
    }

    public async Task<bool> RejectRequestAsync(int id, int rejectedByUserId, string? adminNotes = null)
    {
        try
        {
            var request = await _context.Set<MembershipRequest>().FindAsync(id);
            if (request == null || !request.IsActive)
            {
                return false;
            }

            if (request.Status != MembershipRequestStatus.Pending)
            {
                throw new InvalidOperationException("Sadece bekleyen talepler reddedilebilir.");
            }

            request.Status = MembershipRequestStatus.Rejected;
            request.AdminNotes = adminNotes;
            request.ApprovedBy = rejectedByUserId;
            request.RejectedAt = DateTimeHelper.Now;
            request.UpdatedAt = DateTimeHelper.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi reddedildi. Request ID: {RequestId}, Rejected By: {UserId}", 
                id, rejectedByUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep reddedilirken hata. Request ID: {RequestId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteRequestAsync(int id)
    {
        try
        {
            var request = await _context.Set<MembershipRequest>().FindAsync(id);
            if (request == null || !request.IsActive)
            {
                return false;
            }

            if (request.Status != MembershipRequestStatus.Pending)
            {
                throw new InvalidOperationException("Sadece bekleyen talepler silinebilir.");
            }

            request.IsActive = false;
            request.UpdatedAt = DateTimeHelper.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi silindi. Request ID: {RequestId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep silinirken hata. Request ID: {RequestId}", id);
            return false;
        }
    }
}
