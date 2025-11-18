using AutoMapper;
using GymSystem.Application.Abstractions.Contract.GymLocation;
using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.GymLocations;

public class GymLocationService : IGymLocationService {
    private readonly BaseFactory<GymLocationService> _baseFactory;
    private readonly IServiceResponseHelper _responseHelper;
    private readonly ILogger<GymLocationService> _logger;
    private readonly IMapper _mapper;

    public GymLocationService(BaseFactory<GymLocationService> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public async Task<ServiceResponse<List<GymLocationDto>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            var gymLocations = await repository.QueryNoTracking().Where(g => g.IsActive).OrderBy(g => g.Name).ToListAsync();

            var dtos = _mapper.Map<List<GymLocationDto>>(gymLocations);
            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonları getirilirken hata oluştu");
            return _responseHelper.SetError<List<GymLocationDto>>(null, new ErrorInfo("Spor salonları getirilemedi", "GYMLOCATION_GETALL_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<GymLocationDto?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            var gymLocation = await repository.QueryNoTracking().Where(g => g.Id == id && g.IsActive).FirstOrDefaultAsync();

            if (gymLocation == null)
                return _responseHelper.SetError<GymLocationDto?>(null, "Spor salonu bulunamadı", 404, "GYMLOCATION_NOTFOUND");

            var dto = _mapper.Map<GymLocationDto>(gymLocation);
            return _responseHelper.SetSuccess<GymLocationDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu getirilirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<GymLocationDto?>(null, new ErrorInfo("Spor salonu getirilemedi", "GYMLOCATION_GET_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<GymLocationDto>> CreateAsync(GymLocationDto dto) {
        try {
            var gymLocation = _mapper.Map<GymLocation>(dto, opts => opts.AfterMap((src, dest) => {
                dest.CreatedAt = DateTimeHelper.Now;
                dest.IsActive = true;
            }));

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            await repository.AddAsync(gymLocation);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Spor salonu oluşturuldu. ID: {Id}, İsim: {Name}", gymLocation.Id, gymLocation.Name);

            var responseDto = _mapper.Map<GymLocationDto>(gymLocation);
            return _responseHelper.SetSuccess(responseDto, "Spor salonu oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu oluşturulurken hata oluştu");
            return _responseHelper.SetError<GymLocationDto>(null, new ErrorInfo("Spor salonu oluşturulamadı", "GYMLOCATION_CREATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<GymLocationDto>> UpdateAsync(int id, GymLocationDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            var gymLocation = await repository.Query().Where(g => g.Id == id && g.IsActive).FirstOrDefaultAsync();

            if (gymLocation == null)
                return _responseHelper.SetError<GymLocationDto>(null, "Spor salonu bulunamadı", 404, "GYMLOCATION_NOTFOUND");

            _mapper.Map(dto, gymLocation);
            gymLocation.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(gymLocation);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Spor salonu güncellendi. ID: {Id}, İsim: {Name}", gymLocation.Id, gymLocation.Name);

            var responseDto = _mapper.Map<GymLocationDto>(gymLocation);
            return _responseHelper.SetSuccess(responseDto, "Spor salonu güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu güncellenirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<GymLocationDto>(null, new ErrorInfo("Spor salonu güncellenemedi", "GYMLOCATION_UPDATE_ERROR", ex.StackTrace, 500));
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<GymLocation>();
            var gymLocation = await repository.Query().Where(g => g.Id == id && g.IsActive).FirstOrDefaultAsync();

            if (gymLocation == null)
                return _responseHelper.SetError<bool>(false, "Spor salonu bulunamadı", 404, "GYMLOCATION_NOTFOUND");

            gymLocation.IsActive = false;
            gymLocation.UpdatedAt = DateTimeHelper.Now;

            await repository.UpdateAsync(gymLocation);
            await repository.SaveChangesAsync();

            _logger.LogInformation("Spor salonu silindi. ID: {Id}, İsim: {Name}", id, gymLocation.Name);
            return _responseHelper.SetSuccess(true, "Spor salonu silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu silinirken hata oluştu. ID: {Id}", id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo("Spor salonu silinemedi", "GYMLOCATION_DELETE_ERROR", ex.StackTrace, 500));
        }
    }
}