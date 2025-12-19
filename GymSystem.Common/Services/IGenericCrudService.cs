using GymSystem.Common.Models;

namespace GymSystem.Common.Services;

/// <summary>
/// Generic CRUD operations interface - DTO kullanımı ile
/// TDto: Data Transfer Object
/// </summary>
public interface IGenericCrudService<TDto> where TDto : class {
    Task<ServiceResponse<List<TDto>>> GetAllAsync();
    Task<ServiceResponse<TDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<TDto>> CreateAsync(TDto dto);
    Task<ServiceResponse<TDto>> UpdateAsync(int id, TDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}
