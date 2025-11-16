using GymSystem.Common.Models;

namespace GymSystem.Application.Abstractions.Services;

/// <summary>
/// Generic CRUD operations interface
/// Tüm entity'ler için temel CRUD işlemlerini sağlar
/// NOT: IApplicationService'den TÜREMEZ çünkü generic - manuel registration gerekir
/// </summary>
public interface IGenericCrudService<T> where T : class
{
    Task<ServiceResponse<IEnumerable<T>>> GetAllAsync();
    Task<ServiceResponse<T>> GetByIdAsync(int id);
    Task<ServiceResponse<T>> CreateAsync(T entity);
    Task<ServiceResponse<T>> UpdateAsync(int id, T entity);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}
