using GymSystem.Application.Abstractions.Services;
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

    public MemberService(BaseFactory<MemberService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
    }

    public async Task<ServiceResponse<List<Member>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var members = await repository.QueryNoTracking().Include(m => m.CurrentGymLocation).Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt).ToListAsync();
            return _responseHelper.SetSuccess(members);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member'lar getirilirken hata oluştu");
            return _responseHelper.SetError<List<Member>>(null, new ErrorInfo("Member'lar getirilemedi", "MEMBER_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Member?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await repository.QueryNoTracking().Include(m => m.CurrentGymLocation).Where(m => m.Id == id && m.IsActive).FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<Member?>(null, "Member bulunamadı", 404, "MEMBER_NOTFOUND");

            return _responseHelper.SetSuccess<Member?>(member);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<Member?>(null, new ErrorInfo("Member getirilemedi", "MEMBER_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Member>> CreateAsync(Member entity) {
        try {
            entity.CreatedAt = DateTimeHelper.Now;
            entity.IsActive = true;

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            await repository.AddAsync(entity);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(entity, "Member oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member oluşturulurken hata oluştu");
            return _responseHelper.SetError<Member>(null, new ErrorInfo("Member oluşturulamadı", "MEMBER_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Member>> UpdateAsync(int id, Member entity) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var existingMember = await repository.Query().Where(m => m.Id == id && m.IsActive).FirstOrDefaultAsync();

            if (existingMember == null)
                return _responseHelper.SetError<Member>(null, "Member bulunamadı", 404, "MEMBER_NOTFOUND");

            entity.UpdatedAt = DateTimeHelper.Now;
            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(entity, "Member güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<Member>(null, new ErrorInfo("Member güncellenemedi", "MEMBER_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var member = await repository.Query().Where(m => m.Id == id && m.IsActive).FirstOrDefaultAsync();

            if (member == null)
                return _responseHelper.SetError<bool>(false, "Member bulunamadı", 404, "MEMBER_NOTFOUND");

            member.IsActive = false;
            member.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(member);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(true, "Member silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Member silinemedi", "MEMBER_DELETE_ERROR", ex.StackTrace, 500));
        }
    }

    protected virtual IQueryable<Member> ApplyIncludes(IQueryable<Member> query) {
        return query.Include(m => m.CurrentGymLocation);
    }

    public async Task<ServiceResponse<IEnumerable<Member>>> GetAllMembersWithGymLocationAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Member>();
            var members = await repository.QueryNoTracking().Include(m => m.CurrentGymLocation).Where(m => m.IsActive).OrderByDescending(m => m.CreatedAt).ToListAsync();
            return _responseHelper.SetSuccess<IEnumerable<Member>>(members);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member'lar GymLocation ile birlikte alınırken hata oluştu");
            return _responseHelper.SetError<IEnumerable<Member>>(null, new ErrorInfo("Member'lar alınırken bir hata oluştu", "MEMBER_ERROR_001", ex.StackTrace, 500));
        }
    }
}