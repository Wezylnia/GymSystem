using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Services;

public class ServiceService : IServiceService {
    private readonly BaseFactory<ServiceService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<ServiceService> _logger;

    public ServiceService(BaseFactory<ServiceService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
    }

    public async Task<ServiceResponse<List<Service>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var services = await repository.QueryNoTracking().Include(s => s.GymLocation).Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
            return _responseHelper.SetSuccess(services);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler getirilirken hata oluştu");
            return _responseHelper.SetError<List<Service>>(null, new ErrorInfo("Hizmetler getirilemedi", "SERVICE_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Service?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var service = await repository.QueryNoTracking().Include(s => s.GymLocation).Where(s => s.Id == id && s.IsActive).FirstOrDefaultAsync();

            if (service == null)
                return _responseHelper.SetError<Service?>(null, "Hizmet bulunamadı", 404, "SERVICE_NOTFOUND");

            return _responseHelper.SetSuccess<Service?>(service);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<Service?>(null, new ErrorInfo("Hizmet getirilemedi", "SERVICE_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Service>> CreateAsync(Service entity) {
        try {
            entity.CreatedAt = DateTimeHelper.Now;
            entity.IsActive = true;

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            await repository.AddAsync(entity);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(entity, "Hizmet oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet oluşturulurken hata oluştu");
            return _responseHelper.SetError<Service>(null, new ErrorInfo("Hizmet oluşturulamadı", "SERVICE_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<Service>> UpdateAsync(int id, Service entity) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var existingService = await repository.Query().Where(s => s.Id == id && s.IsActive).FirstOrDefaultAsync();

            if (existingService == null)
                return _responseHelper.SetError<Service>(null, "Hizmet bulunamadı", 404, "SERVICE_NOTFOUND");

            entity.UpdatedAt = DateTimeHelper.Now;
            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(entity, "Hizmet güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<Service>(null, new ErrorInfo("Hizmet güncellenemedi", "SERVICE_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var service = await repository.Query().Where(s => s.Id == id && s.IsActive).FirstOrDefaultAsync();

            if (service == null)
                return _responseHelper.SetError<bool>(false, "Hizmet bulunamadı", 404, "SERVICE_NOTFOUND");

            service.IsActive = false;
            service.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(service);
            await repository.SaveChangesAsync();

            return _responseHelper.SetSuccess(true, "Hizmet silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Hizmet silinemedi", "SERVICE_DELETE_ERROR", ex.StackTrace, 500));
        }
    }
}