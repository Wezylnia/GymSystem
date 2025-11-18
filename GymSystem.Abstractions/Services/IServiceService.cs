using GymSystem.Application.Abstractions.Contract.Service;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services;

/// <summary>
/// Service service interface - DTO kullanımı ile
/// </summary>
public interface IServiceService : IApplicationService
{
    Task<ServiceResponse<List<ServiceDto>>> GetAllAsync();
    Task<ServiceResponse<ServiceDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<ServiceDto>> CreateAsync(ServiceDto dto);
    Task<ServiceResponse<ServiceDto>> UpdateAsync(int id, ServiceDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}