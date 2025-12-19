using AutoMapper;
using GymSystem.Application.Abstractions.Services.IServiceService;
using GymSystem.Application.Abstractions.Services.IServiceService.Contract;
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
    private readonly IMapper _mapper;

    public ServiceService(BaseFactory<ServiceService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<List<ServiceDto>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var services = await repository.QueryNoTracking().Include(s => s.GymLocation).Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

            var dtos = _mapper.Map<List<ServiceDto>>(services);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmetler getirilirken hata oluştu");
            return _responseHelper.SetError<List<ServiceDto>>(null, new ErrorInfo("Hizmetler getirilemedi", "SERVICE_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<ServiceDto?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var service = await repository.QueryNoTracking().Include(s => s.GymLocation).Where(s => s.Id == id && s.IsActive).FirstOrDefaultAsync();

            if (service == null)
                return _responseHelper.SetError<ServiceDto?>(null, "Hizmet bulunamadı", 404, "SERVICE_NOTFOUND");

            var dto = _mapper.Map<ServiceDto>(service);
            return _responseHelper.SetSuccess<ServiceDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<ServiceDto?>(null, new ErrorInfo("Hizmet getirilemedi", "SERVICE_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<ServiceDto>> CreateAsync(ServiceDto dto) {
        try {
            var service = _mapper.Map<Service>(dto, opts => opts.AfterMap((src, dest) => {
                dest.CreatedAt = DateTimeHelper.Now;
                dest.IsActive = true;
            }));

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            await repository.AddAsync(service);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Hizmet oluşturuldu. ID: {Id}, İsim: {Name}", service.Id, service.Name);

            var responseDto = _mapper.Map<ServiceDto>(service);
            return _responseHelper.SetSuccess(responseDto, "Hizmet oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet oluşturulurken hata oluştu");
            return _responseHelper.SetError<ServiceDto>(null, new ErrorInfo("Hizmet oluşturulamadı", "SERVICE_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<ServiceDto>> UpdateAsync(int id, ServiceDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<Service>();
            var service = await repository.Query().Where(s => s.Id == id && s.IsActive).FirstOrDefaultAsync();

            if (service == null)
                return _responseHelper.SetError<ServiceDto>(null, "Hizmet bulunamadı", 404, "SERVICE_NOTFOUND");

            _mapper.Map(dto, service);
            service.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(service);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Hizmet güncellendi. ID: {Id}, İsim: {Name}", service.Id, service.Name);

            var responseDto = _mapper.Map<ServiceDto>(service);
            return _responseHelper.SetSuccess(responseDto, "Hizmet güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<ServiceDto>(null, new ErrorInfo("Hizmet güncellenemedi", "SERVICE_UPDATE_ERROR", ex.StackTrace, 500));
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

            _logger.LogInformation("Hizmet silindi. ID: {Id}, İsim: {Name}", id, service.Name);
            return _responseHelper.SetSuccess(true, "Hizmet silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Hizmet silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Hizmet silinemedi", "SERVICE_DELETE_ERROR", ex.StackTrace, 500));
        }
    }
}