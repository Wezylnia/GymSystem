using AutoMapper;
using GymSystem.Application.Abstractions.Services.IMembershipRequestService;
using GymSystem.Application.Abstractions.Services.IMembershipRequestService.Contract;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Membership;

/// <summary>
/// Üyelik talepleri servisi - DTO + AutoMapper + ServiceResponse pattern
/// </summary>
public class MembershipRequestService : IMembershipRequestService {
    private readonly BaseFactory<MembershipRequestService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<MembershipRequestService> _logger;
    private readonly IMapper _mapper;

    public MembershipRequestService(BaseFactory<MembershipRequestService> baseFactory, ILogger<MembershipRequestService> logger) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = logger;
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<MembershipRequestDto>> CreateRequestAsync(int memberId, int gymLocationId, MembershipDuration duration, decimal price, string? notes = null) {
        try {
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var gymLocationRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();

            var member = await memberRepository.QueryNoTracking().Where(m => m.Id == memberId).Select(m => new { m.Id, m.MembershipEndDate }).FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<MembershipRequestDto>(null, $"Member ID {memberId} bulunamadı", 404, "MEMBER_NOTFOUND");

            if (member.MembershipEndDate.HasValue && member.MembershipEndDate.Value > DateTime.Now) {
                var daysRemaining = (member.MembershipEndDate.Value - DateTime.Now).Days;
                return _responseHelper.SetError<MembershipRequestDto>(null, $"Zaten aktif bir üyeliğiniz bulunmaktadır. Üyeliğiniz {member.MembershipEndDate:dd.MM.yyyy} tarihinde sona erecek ({daysRemaining} gün kaldı). Mevcut üyeliğiniz bitmeden yeni talep oluşturamazsınız.", 400, "ACTIVE_MEMBERSHIP_EXISTS");
            }

            var gymExists = await gymLocationRepository.QueryNoTracking().AnyAsync(g => g.Id == gymLocationId && g.IsActive);
            if (!gymExists)
                return _responseHelper.SetError<MembershipRequestDto>(null, $"Gym Location ID {gymLocationId} bulunamadı", 404, "GYMLOCATION_NOTFOUND");

            var hasPendingRequest = await requestRepository.QueryNoTracking().AnyAsync(mr => mr.MemberId == memberId && mr.GymLocationId == gymLocationId && mr.Status == MembershipRequestStatus.Pending && mr.IsActive);
            if (hasPendingRequest)
                return _responseHelper.SetError<MembershipRequestDto>(null, "Bu salon için zaten bekleyen bir talebiniz var", 400, "PENDING_REQUEST_EXISTS");

            var request = new MembershipRequest { MemberId = memberId, GymLocationId = gymLocationId, Duration = duration, Price = price, Notes = notes, Status = MembershipRequestStatus.Pending, CreatedAt = DateTimeHelper.Now, IsActive = true };

            await requestRepository.AddAsync(request);
            await requestRepository.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi oluşturuldu. Member ID: {MemberId}, Gym ID: {GymId}, Request ID: {RequestId}", memberId, gymLocationId, request.Id);

            var dto = _mapper.Map<MembershipRequestDto>(request);
            return _responseHelper.SetSuccess(dto, "Üyelik talebi oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üyelik talebi oluşturulurken hata. Member ID: {MemberId}", memberId);
            return _responseHelper.SetError<MembershipRequestDto>(null, new ErrorInfo("Üyelik talebi oluşturulamadı", "MEMBERSHIPREQUEST_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<MembershipRequestDto>>> GetMemberRequestsAsync(int memberId) {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var requests = await requestRepository.QueryNoTracking().Include(mr => mr.GymLocation).Include(mr => mr.Member).Where(mr => mr.MemberId == memberId && mr.IsActive).OrderByDescending(mr => mr.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<MembershipRequestDto>>(requests);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye talepleri getirilirken hata. Member ID: {MemberId}", memberId);
            return _responseHelper.SetError<List<MembershipRequestDto>>(null, new ErrorInfo("Üye talepleri getirilemedi", "MEMBERSHIPREQUEST_GETMEMBER_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<MembershipRequestDto>>> GetGymLocationRequestsAsync(int gymLocationId) {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var requests = await requestRepository.QueryNoTracking().Include(mr => mr.Member).Include(mr => mr.GymLocation).Where(mr => mr.GymLocationId == gymLocationId && mr.IsActive).OrderByDescending(mr => mr.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<MembershipRequestDto>>(requests);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Salon talepleri getirilirken hata. Gym ID: {GymId}", gymLocationId);
            return _responseHelper.SetError<List<MembershipRequestDto>>(null, new ErrorInfo("Salon talepleri getirilemedi", "MEMBERSHIPREQUEST_GETGYM_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<MembershipRequestDto?>> GetRequestByIdAsync(int id) {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var request = await requestRepository.QueryNoTracking().Include(mr => mr.Member).Include(mr => mr.GymLocation).Where(mr => mr.Id == id && mr.IsActive).FirstOrDefaultAsync();

            if (request == null)
                return _responseHelper.SetError<MembershipRequestDto?>(null, "Talep bulunamadı", 404, "MEMBERSHIPREQUEST_NOTFOUND");

            var dto = _mapper.Map<MembershipRequestDto>(request);
            return _responseHelper.SetSuccess<MembershipRequestDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep getirilirken hata. Request ID: {RequestId}", id);
            return _responseHelper.SetError<MembershipRequestDto?>(null, new ErrorInfo("Talep getirilemedi", "MEMBERSHIPREQUEST_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<MembershipRequestDto>>> GetAllRequestsAsync() {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var requests = await requestRepository.QueryNoTracking().Include(mr => mr.Member).Include(mr => mr.GymLocation).Where(mr => mr.IsActive).OrderByDescending(mr => mr.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<MembershipRequestDto>>(requests);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Tüm talepler getirilirken hata");
            return _responseHelper.SetError<List<MembershipRequestDto>>(null, new ErrorInfo("Talepler getirilemedi", "MEMBERSHIPREQUEST_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<MembershipRequestDto>>> GetPendingRequestsAsync() {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var requests = await requestRepository.QueryNoTracking().Include(mr => mr.Member).Include(mr => mr.GymLocation).Where(mr => mr.Status == MembershipRequestStatus.Pending && mr.IsActive).OrderByDescending(mr => mr.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<MembershipRequestDto>>(requests);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Bekleyen talepler getirilirken hata");
            return _responseHelper.SetError<List<MembershipRequestDto>>(null, new ErrorInfo("Bekleyen talepler getirilemedi", "MEMBERSHIPREQUEST_GETPENDING_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> ApproveRequestAsync(int id, int approvedByUserId, string? adminNotes = null) {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();

            var request = await requestRepository.Query().Where(r => r.Id == id && r.IsActive).FirstOrDefaultAsync();

            if (request == null)
                return _responseHelper.SetError<bool>(false, "Talep bulunamadı", 404, "MEMBERSHIPREQUEST_NOTFOUND");

            if (request.Status != MembershipRequestStatus.Pending)
                return _responseHelper.SetError<bool>(false, "Sadece bekleyen talepler onaylanabilir", 400, "INVALID_STATUS");

            request.Status = MembershipRequestStatus.Approved;
            request.AdminNotes = adminNotes;
            request.ApprovedBy = approvedByUserId;
            request.ApprovedAt = DateTimeHelper.Now;
            request.UpdatedAt = DateTimeHelper.Now;

            var member = await memberRepository.Query().Where(m => m.Id == request.MemberId).FirstOrDefaultAsync();

            if (member != null) {
                var now = DateTimeHelper.Now;
                var startDate = member.MembershipEndDate > now ? member.MembershipEndDate.Value : now;
                member.MembershipStartDate = startDate;
                member.MembershipEndDate = startDate.AddMonthsSafe((int)request.Duration);
                member.CurrentGymLocationId = request.GymLocationId;
                member.IsActive = true;
                member.UpdatedAt = DateTimeHelper.Now;

                await memberRepository.UpdateAsync(member);
            }

            await requestRepository.UpdateAsync(request);
            await requestRepository.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi onaylandı. Request ID: {RequestId}, Approved By: {UserId}", id, approvedByUserId);
            return _responseHelper.SetSuccess(true, "Üyelik talebi onaylandı");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep onaylanırken hata. Request ID: {RequestId}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Talep onaylanamadı", "MEMBERSHIPREQUEST_APPROVE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> RejectRequestAsync(int id, int rejectedByUserId, string? adminNotes = null) {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var request = await requestRepository.Query().Where(r => r.Id == id && r.IsActive).FirstOrDefaultAsync();

            if (request == null)
                return _responseHelper.SetError<bool>(false, "Talep bulunamadı", 404, "MEMBERSHIPREQUEST_NOTFOUND");

            if (request.Status != MembershipRequestStatus.Pending)
                return _responseHelper.SetError<bool>(false, "Sadece bekleyen talepler reddedilebilir", 400, "INVALID_STATUS");

            request.Status = MembershipRequestStatus.Rejected;
            request.AdminNotes = adminNotes;
            request.ApprovedBy = rejectedByUserId;
            request.RejectedAt = DateTimeHelper.Now;
            request.UpdatedAt = DateTimeHelper.Now;

            await requestRepository.UpdateAsync(request);
            await requestRepository.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi reddedildi. Request ID: {RequestId}, Rejected By: {UserId}", id, rejectedByUserId);
            return _responseHelper.SetSuccess(true, "Üyelik talebi reddedildi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep reddedilirken hata. Request ID: {RequestId}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Talep reddedilemedi", "MEMBERSHIPREQUEST_REJECT_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteRequestAsync(int id) {
        try {
            var requestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var request = await requestRepository.Query().Where(r => r.Id == id && r.IsActive).FirstOrDefaultAsync();

            if (request == null)
                return _responseHelper.SetError<bool>(false, "Talep bulunamadı", 404, "MEMBERSHIPREQUEST_NOTFOUND");

            if (request.Status != MembershipRequestStatus.Pending)
                return _responseHelper.SetError<bool>(false, "Sadece bekleyen talepler silinebilir", 400, "INVALID_STATUS");

            request.IsActive = false;
            request.UpdatedAt = DateTimeHelper.Now;

            await requestRepository.UpdateAsync(request);
            await requestRepository.SaveChangesAsync();

            _logger.LogInformation("Üyelik talebi silindi. Request ID: {RequestId}", id);
            return _responseHelper.SetSuccess(true, "Üyelik talebi silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep silinirken hata. Request ID: {RequestId}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Talep silinemedi", "MEMBERSHIPREQUEST_DELETE_ERROR", ex.StackTrace, 500));
        }
    }
}