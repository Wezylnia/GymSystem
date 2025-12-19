using AutoMapper;
using GymSystem.Application.Abstractions.Services.IMemberService;
using GymSystem.Application.Abstractions.Services.IMemberService.Contract;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Members;

public class MemberService : IMemberService {
    private readonly BaseFactory<MemberService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<MemberService> _logger;
    private readonly IMapper _mapper;

    public MemberService(BaseFactory<MemberService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<List<MemberDto>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var members = await repository.QueryNoTracking().Include(m => m.CurrentGymLocation).Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<MemberDto>>(members);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member'lar getirilirken hata oluştu");
            return _responseHelper.SetError<List<MemberDto>>(null, new ErrorInfo("Member'lar getirilemedi", "MEMBER_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<MemberDto?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await repository.QueryNoTracking().Include(m => m.CurrentGymLocation).Where(m => m.Id == id && m.IsActive).FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<MemberDto?>(null, "Member bulunamadı", 404, "MEMBER_NOTFOUND");

            var dto = _mapper.Map<MemberDto>(member);
            return _responseHelper.SetSuccess<MemberDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<MemberDto?>(null, new ErrorInfo("Member getirilemedi", "MEMBER_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<MemberDto>> CreateAsync(MemberDto dto) {
        try {
            var member = _mapper.Map<Member>(dto, opts => opts.AfterMap((src, dest) => {
                dest.CreatedAt = DateTimeHelper.Now;
                dest.IsActive = true;
            }));

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            await repository.AddAsync(member);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Member oluşturuldu. ID: {Id}, İsim: {FirstName} {LastName}", member.Id, member.FirstName, member.LastName);

            var responseDto = _mapper.Map<MemberDto>(member);
            return _responseHelper.SetSuccess(responseDto, "Member oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member oluşturulurken hata oluştu");
            return _responseHelper.SetError<MemberDto>(null, new ErrorInfo("Member oluşturulamadı", "MEMBER_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<MemberDto>> UpdateAsync(int id, MemberDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await repository.Query().Where(m => m.Id == id && m.IsActive).FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<MemberDto>(null, "Member bulunamadı", 404, "MEMBER_NOTFOUND");

            _mapper.Map(dto, member);
            member.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(member);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Member güncellendi. ID: {Id}, İsim: {FirstName} {LastName}", member.Id, member.FirstName, member.LastName);

            var responseDto = _mapper.Map<MemberDto>(member);
            return _responseHelper.SetSuccess(responseDto, "Member güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<MemberDto>(null, new ErrorInfo("Member güncellenemedi", "MEMBER_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var memberRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await memberRepository.Query().Where(m => m.Id == id && m.IsActive).FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<bool>(false, "Member bulunamadı", 404, "MEMBER_NOTFOUND");

            // Cascade Delete: İlişkili kayıtları sil
            _logger.LogInformation("Member silme işlemi başlatılıyor. ID: {Id}, İsim: {FirstName} {LastName}", id, member.FirstName, member.LastName);

            // 1. AI Workout Plans sil
            var aiPlanRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<AIWorkoutPlan>();
            var aiPlans = await aiPlanRepository.Query().Where(p => p.MemberId == id && p.IsActive).ToListAsync();
            foreach (var plan in aiPlans) {
                plan.IsActive = false;
                plan.UpdatedAt = DateTimeHelper.Now;
                await aiPlanRepository.UpdateAsync(plan);
            }
            _logger.LogInformation("AI Planları silindi. Count: {Count}", aiPlans.Count);

            // 2. Membership Requests sil
            var membershipRequestRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<MembershipRequest>();
            var requests = await membershipRequestRepository.Query().Where(r => r.MemberId == id && r.IsActive).ToListAsync();
            foreach (var request in requests) {
                request.IsActive = false;
                request.UpdatedAt = DateTimeHelper.Now;
                await membershipRequestRepository.UpdateAsync(request);
            }
            _logger.LogInformation("Üyelik talepleri silindi. Count: {Count}", requests.Count);

            // 3. Appointments sil
            var appointmentRepository = _baseFactory.CreateRepositoryFactory().CreateRepository<Appointment>();
            var appointments = await appointmentRepository.Query().Where(a => a.MemberId == id && a.IsActive).ToListAsync();
            foreach (var appointment in appointments) {
                appointment.IsActive = false;
                appointment.UpdatedAt = DateTimeHelper.Now;
                await appointmentRepository.UpdateAsync(appointment);
            }
            _logger.LogInformation("Randevular silindi. Count: {Count}", appointments.Count);

            // 4. Member'ı sil
            member.IsActive = false;
            member.UpdatedAt = DateTimeHelper.Now;
            await memberRepository.UpdateAsync(member);
            await memberRepository.SaveChangesAsync();

            _logger.LogInformation("Member ve tüm ilişkili kayıtlar silindi. ID: {Id}", id);
            return _responseHelper.SetSuccess(true, $"Member ve {aiPlans.Count} AI planı, {requests.Count} üyelik talebi, {appointments.Count} randevu silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Member silinemedi", "MEMBER_DELETE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<MemberDto>>> GetAllMembersWithGymLocationAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var members = await repository.QueryNoTracking().Include(m => m.CurrentGymLocation).Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt).ToListAsync();

            var dtos = _mapper.Map<List<MemberDto>>(members);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member'lar GymLocation ile birlikte alınırken hata oluştu");
            return _responseHelper.SetError<List<MemberDto>>(null, new ErrorInfo("Member'lar alınırken bir hata oluştu", "MEMBER_ERROR_001", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<MemberDto?>> GetByEmailAsync(string email) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await repository.QueryNoTracking()
                .Include(m => m.CurrentGymLocation)
                .Where(m => m.Email == email && m.IsActive)
                .FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<MemberDto?>(null, $"Email ile Member bulunamadı: {email}", 404, "MEMBER_NOTFOUND_BYEMAIL");

            var dto = _mapper.Map<MemberDto>(member);
            return _responseHelper.SetSuccess<MemberDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member email ile alınırken hata. Email: {Email}", email);
            return _responseHelper.SetError<MemberDto?>(null, new ErrorInfo("Member getirilemedi", "MEMBER_GET_BYEMAIL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<List<MemberDto>>> GetMembersByGymLocationAsync(int gymLocationId) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var members = await repository.QueryNoTracking()
                .Include(m => m.CurrentGymLocation)
                .Where(m => m.CurrentGymLocationId == gymLocationId && m.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<MemberDto>>(members);
            _logger.LogInformation("GymLocation ID {GymLocationId} için {Count} member getirildi", gymLocationId, dtos.Count);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "GymLocation üyeleri alınırken hata. GymLocation ID: {GymLocationId}", gymLocationId);
            return _responseHelper.SetError<List<MemberDto>>(null, new ErrorInfo("GymLocation üyeleri getirilemedi", "MEMBER_GET_BYGYMLOCATION_ERROR", ex.StackTrace, 500));
        }
    }
}