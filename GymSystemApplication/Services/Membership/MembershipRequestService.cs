using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Membership;

/// <summary>
/// Üyelik talepleri servisi - LINQ optimized
/// Repository pattern kullanır, DbContext'e direkt erişim yok
/// </summary>
public class MembershipRequestService : IMembershipRequestService
{
    private readonly BaseFactory<MembershipRequestService> _baseFactory;
    private readonly ILogger<MembershipRequestService> _logger;

    public MembershipRequestService(
        BaseFactory<MembershipRequestService> baseFactory,
        ILogger<MembershipRequestService> logger)
    {
        _baseFactory = baseFactory;
        _logger = logger;
    }

    public async Task<MembershipRequest> CreateRequestAsync(int memberId, int gymLocationId, 
        MembershipDuration duration, decimal price, string? notes = null)
    {
        try
        {
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var gymLocationRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();

            // Member kontrolü - LINQ
            var member = await memberRepository.QueryNoTracking()
                .Where(m => m.Id == memberId)
                .Select(m => new { m.Id, m.MembershipEndDate }) // Projection - sadece gerekli alanlar
                .FirstOrDefaultAsync();

            if (member == null)
            {
                throw new ArgumentException($"Member ID {memberId} bulunamadı.");
            }

            // Aktif üyelik kontrolü
            if (member.MembershipEndDate.HasValue && member.MembershipEndDate.Value > DateTime.Now)
            {
                var daysRemaining = (member.MembershipEndDate.Value - DateTime.Now).Days;
                throw new InvalidOperationException(
                    $"Zaten aktif bir üyeliğiniz bulunmaktadır. " +
                    $"Üyeliğiniz {member.MembershipEndDate:dd.MM.yyyy} tarihinde sona erecek " +
                    $"({daysRemaining} gün kaldı). " +
                    $"Mevcut üyeliğiniz bitmeden yeni talep oluşturamazsınız.");
            }

            // GymLocation kontrolü - AnyAsync
            var gymExists = await gymLocationRepository.QueryNoTracking()
                .AnyAsync(g => g.Id == gymLocationId);

            if (!gymExists)
            {
                throw new ArgumentException($"Gym Location ID {gymLocationId} bulunamadı.");
            }

            // Aynı salon için bekleyen talep kontrolü - AnyAsync
            var hasPendingRequest = await requestRepository.QueryNoTracking()
                .AnyAsync(mr => 
                    mr.MemberId == memberId && 
                    mr.GymLocationId == gymLocationId && 
                    mr.Status == MembershipRequestStatus.Pending &&
                    mr.IsActive);

            if (hasPendingRequest)
            {
                throw new InvalidOperationException("Bu salon için zaten bekleyen bir talebiniz var.");
            }

            // Request oluştur
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

            await requestRepository.AddAsync(request);
            await requestRepository.SaveChangesAsync();

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
        var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
        
        // LINQ - IQueryable with Include
        return await requestRepository.QueryNoTracking()
            .Include(mr => mr.GymLocation)
            .Include(mr => mr.Member)
            .Where(mr => mr.MemberId == memberId && mr.IsActive)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MembershipRequest>> GetGymLocationRequestsAsync(int gymLocationId)
    {
        var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
        
        return await requestRepository.QueryNoTracking()
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .Where(mr => mr.GymLocationId == gymLocationId && mr.IsActive)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<MembershipRequest?> GetRequestByIdAsync(int id)
    {
        var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
        
        return await requestRepository.QueryNoTracking()
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .Where(mr => mr.Id == id && mr.IsActive)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MembershipRequest>> GetAllRequestsAsync()
    {
        var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
        
        return await requestRepository.QueryNoTracking()
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .Where(mr => mr.IsActive)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MembershipRequest>> GetPendingRequestsAsync()
    {
        var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
        
        return await requestRepository.QueryNoTracking()
            .Include(mr => mr.Member)
            .Include(mr => mr.GymLocation)
            .Where(mr => mr.Status == MembershipRequestStatus.Pending && mr.IsActive)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveRequestAsync(int id, int approvedByUserId, string? adminNotes = null)
    {
        try
        {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();

            // Request getir - tracking enabled (update için)
            var request = await requestRepository.Query()
                .Where(r => r.Id == id && r.IsActive)
                .FirstOrDefaultAsync();

            if (request == null)
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

            // Member güncelle - tracking enabled
            var member = await memberRepository.Query()
                .Where(m => m.Id == request.MemberId)
                .FirstOrDefaultAsync();

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

                await memberRepository.UpdateAsync(member);
            }

            await requestRepository.UpdateAsync(request);
            await requestRepository.SaveChangesAsync();

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
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();

            var request = await requestRepository.Query()
                .Where(r => r.Id == id && r.IsActive)
                .FirstOrDefaultAsync();

            if (request == null)
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

            await requestRepository.UpdateAsync(request);
            await requestRepository.SaveChangesAsync();

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
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();

            var request = await requestRepository.Query()
                .Where(r => r.Id == id && r.IsActive)
                .FirstOrDefaultAsync();

            if (request == null)
            {
                return false;
            }

            if (request.Status != MembershipRequestStatus.Pending)
            {
                throw new InvalidOperationException("Sadece bekleyen talepler silinebilir.");
            }

            request.IsActive = false;
            request.UpdatedAt = DateTimeHelper.Now;

            await requestRepository.UpdateAsync(request);
            await requestRepository.SaveChangesAsync();

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