using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using Microsoft.Extensions.Logging;

namespace GymSystem.Common.Services;

/// <summary>
/// Generic CRUD service implementation
/// Factory pattern ile repository ve utility'lere erişim sağlar
/// </summary>
public class GenericCrudService<T> : IGenericCrudService<T> where T : class
{
    protected readonly BaseFactory<GenericCrudService<T>> _baseFactory;
    protected readonly IServiceResponseHelper _responseHelper;
    protected readonly ILogger<GenericCrudService<T>> _logger;

    public GenericCrudService(BaseFactory<GenericCrudService<T>> baseFactory)
    {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
    }

    public virtual async Task<ServiceResponse<IEnumerable<T>>> GetAllAsync()
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<T>();
            var entities = await repository.GetAllAsync();

            return _responseHelper.SetSuccess<IEnumerable<T>>(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType}", typeof(T).Name);
            var errorInfo = new ErrorInfo(
                $"{typeof(T).Name} listesi getirilirken hata oluştu",
                "GENERIC_001",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<IEnumerable<T>>(null, errorInfo);
        }
    }

    public virtual async Task<ServiceResponse<T>> GetByIdAsync(int id)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<T>();
            var entity = await repository.GetByIdAsync(id);

            if (entity == null)
            {
                return _responseHelper.SetError<T>(
                    null,
                    $"{typeof(T).Name} bulunamadı. ID: {id}",
                    404,
                    "GENERIC_002");
            }

            return _responseHelper.SetSuccess(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} with ID {Id}", typeof(T).Name, id);
            var errorInfo = new ErrorInfo(
                $"{typeof(T).Name} getirilirken hata oluştu. ID: {id}",
                "GENERIC_003",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<T>(null, errorInfo);
        }
    }

    public virtual async Task<ServiceResponse<T>> CreateAsync(T entity)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<T>();

            await repository.AddAsync(entity);
            await repository.SaveChangesAsync();

            _logger.LogInformation("{EntityType} created successfully", typeof(T).Name);
            return _responseHelper.SetSuccess(entity, $"{typeof(T).Name} başarıyla oluşturuldu");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityType}", typeof(T).Name);
            var errorInfo = new ErrorInfo(
                $"{typeof(T).Name} oluşturulurken hata oluştu",
                "GENERIC_004",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<T>(null, errorInfo);
        }
    }

    public virtual async Task<ServiceResponse<T>> UpdateAsync(int id, T entity)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<T>();
            var existingEntity = await repository.GetByIdAsync(id);

            if (existingEntity == null)
            {
                return _responseHelper.SetError<T>(
                    null,
                    $"Güncellenecek {typeof(T).Name} bulunamadı. ID: {id}",
                    404,
                    "GENERIC_005");
            }

            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            _logger.LogInformation("{EntityType} with ID {Id} updated successfully", typeof(T).Name, id);
            return _responseHelper.SetSuccess(entity, $"{typeof(T).Name} başarıyla güncellendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType} with ID {Id}", typeof(T).Name, id);
            var errorInfo = new ErrorInfo(
                $"{typeof(T).Name} güncellenirken hata oluştu. ID: {id}",
                "GENERIC_006",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<T>(null, errorInfo);
        }
    }

    public virtual async Task<ServiceResponse<bool>> DeleteAsync(int id)
    {
        try
        {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<T>();
            var result = await repository.DeleteAsync(id);

            if (!result)
            {
                return _responseHelper.SetError<bool>(
                    false,
                    $"Silinecek {typeof(T).Name} bulunamadı. ID: {id}",
                    404,
                    "GENERIC_007");
            }

            _logger.LogInformation("{EntityType} with ID {Id} deleted successfully", typeof(T).Name, id);
            return _responseHelper.SetSuccess(true, $"{typeof(T).Name} başarıyla silindi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} with ID {Id}", typeof(T).Name, id);
            var errorInfo = new ErrorInfo(
                $"{typeof(T).Name} silinirken hata oluştu. ID: {id}",
                "GENERIC_008",
                ex.StackTrace,
                500);
            return _responseHelper.SetError<bool>(false, errorInfo);
        }
    }
}
