using AutoMapper;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Helpers;
using GymSystem.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Common.Services;

/// <summary>
/// Generic CRUD service implementation - DTO + AutoMapper + ServiceResponse pattern
/// TEntity: Database entity, TDto: Data Transfer Object
/// Factory pattern ile repository ve utility'lere erişim sağlar
/// </summary>
public abstract class GenericCrudService<TEntity, TDto> : IGenericCrudService<TDto>
    where TEntity : class
    where TDto : class {
    protected readonly BaseFactory<GenericCrudService<TEntity, TDto>> _baseFactory;
    protected readonly IServiceResponseHelper _responseHelper;
    protected readonly ILogger<GenericCrudService<TEntity, TDto>> _logger;
    protected readonly IMapper _mapper;

    protected GenericCrudService(BaseFactory<GenericCrudService<TEntity, TDto>> baseFactory) {
        _baseFactory = baseFactory;
        _responseHelper = baseFactory.CreateUtilityFactory().CreateServiceResponseHelper();
        _logger = baseFactory.CreateUtilityFactory().CreateLogger();
        _mapper = baseFactory.CreateUtilityFactory().CreateMapper();
    }

    public virtual async Task<ServiceResponse<List<TDto>>> GetAllAsync() {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<TEntity>();
            var query = repository.QueryNoTracking();
            query = ApplyIncludes(query);
            query = ApplyFilters(query);
            query = ApplySorting(query);

            var entities = await query.ToListAsync();
            var dtos = _mapper.Map<List<TDto>>(entities);

            return _responseHelper.SetSuccess(dtos);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error getting all {EntityType}", typeof(TEntity).Name);
            return _responseHelper.SetError<List<TDto>>(null, new ErrorInfo($"{typeof(TEntity).Name} listesi getirilirken hata oluştu", "GENERIC_001", ex.StackTrace, 500));
        }
    }

    public virtual async Task<ServiceResponse<TDto?>> GetByIdAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<TEntity>();
            var query = repository.QueryNoTracking();
            query = ApplyIncludes(query);

            var entity = await query.Where(e => EF.Property<int>(e, "Id") == id).FirstOrDefaultAsync();

            if (entity == null)
                return _responseHelper.SetError<TDto?>(null, $"{typeof(TEntity).Name} bulunamadı. ID: {id}", 404, "GENERIC_002");

            var dto = _mapper.Map<TDto>(entity);
            return _responseHelper.SetSuccess<TDto?>(dto);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error getting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
            return _responseHelper.SetError<TDto?>(null, new ErrorInfo($"{typeof(TEntity).Name} getirilirken hata oluştu. ID: {id}", "GENERIC_003", ex.StackTrace, 500));
        }
    }

    public virtual async Task<ServiceResponse<TDto>> CreateAsync(TDto dto) {
        try {
            var entity = _mapper.Map<TEntity>(dto, opts => opts.AfterMap((src, dest) => {
                OnBeforeCreate(dest);
            }));

            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<TEntity>();
            await repository.AddAsync(entity);
            await repository.SaveChangesAsync();

            _logger.LogInformation("{EntityType} created successfully", typeof(TEntity).Name);

            var responseDto = _mapper.Map<TDto>(entity);
            return _responseHelper.SetSuccess(responseDto, $"{typeof(TEntity).Name} başarıyla oluşturuldu");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error creating {EntityType}", typeof(TEntity).Name);
            return _responseHelper.SetError<TDto>(null, new ErrorInfo($"{typeof(TEntity).Name} oluşturulurken hata oluştu", "GENERIC_004", ex.StackTrace, 500));
        }
    }

    public virtual async Task<ServiceResponse<TDto>> UpdateAsync(int id, TDto dto) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<TEntity>();
            var entity = await repository.Query().Where(e => EF.Property<int>(e, "Id") == id).FirstOrDefaultAsync();

            if (entity == null)
                return _responseHelper.SetError<TDto>(null, $"Güncellenecek {typeof(TEntity).Name} bulunamadı. ID: {id}", 404, "GENERIC_005");

            _mapper.Map(dto, entity);
            OnBeforeUpdate(entity);

            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            _logger.LogInformation("{EntityType} with ID {Id} updated successfully", typeof(TEntity).Name, id);

            var responseDto = _mapper.Map<TDto>(entity);
            return _responseHelper.SetSuccess(responseDto, $"{typeof(TEntity).Name} başarıyla güncellendi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error updating {EntityType} with ID {Id}", typeof(TEntity).Name, id);
            return _responseHelper.SetError<TDto>(null, new ErrorInfo($"{typeof(TEntity).Name} güncellenirken hata oluştu. ID: {id}", "GENERIC_006", ex.StackTrace, 500));
        }
    }

    public virtual async Task<ServiceResponse<bool>> DeleteAsync(int id) {
        try {
            var repository = _baseFactory.CreateRepositoryFactory().CreateRepository<TEntity>();
            var entity = await repository.Query().Where(e => EF.Property<int>(e, "Id") == id).FirstOrDefaultAsync();

            if (entity == null)
                return _responseHelper.SetError<bool>(false, $"Silinecek {typeof(TEntity).Name} bulunamadı. ID: {id}", 404, "GENERIC_007");

            OnBeforeDelete(entity);

            await repository.UpdateAsync(entity);
            await repository.SaveChangesAsync();

            _logger.LogInformation("{EntityType} with ID {Id} deleted successfully", typeof(TEntity).Name, id);
            return _responseHelper.SetSuccess(true, $"{typeof(TEntity).Name} başarıyla silindi");
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error deleting {EntityType} with ID {Id}", typeof(TEntity).Name, id);
            return _responseHelper.SetError<bool>(false, new ErrorInfo($"{typeof(TEntity).Name} silinirken hata oluştu. ID: {id}", "GENERIC_008", ex.StackTrace, 500));
        }
    }

    /// <summary>
    /// Override edilecek - Navigation property'ler için Include'lar ekle
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query) => query;

    /// <summary>
    /// Override edilecek - Filtreleme (örn: IsActive = true)
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyFilters(IQueryable<TEntity> query) => query;

    /// <summary>
    /// Override edilecek - Sıralama (örn: OrderBy name, OrderByDescending createdAt)
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySorting(IQueryable<TEntity> query) => query;

    /// <summary>
    /// Override edilecek - Create işleminden önce çalışır (örn: CreatedAt, IsActive set et)
    /// </summary>
    protected virtual void OnBeforeCreate(TEntity entity) { }

    /// <summary>
    /// Override edilecek - Update işleminden önce çalışır (örn: UpdatedAt set et)
    /// </summary>
    protected virtual void OnBeforeUpdate(TEntity entity) { }

    /// <summary>
    /// Override edilecek - Delete işleminden önce çalışır (örn: Soft delete için IsActive = false)
    /// </summary>
    protected virtual void OnBeforeDelete(TEntity entity) { }
}