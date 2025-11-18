using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface IServiceService : IApplicationService {
    Task<ServiceResponse<List<Service>>> GetAllAsync();
    Task<ServiceResponse<Service?>> GetByIdAsync(int id);
    Task<ServiceResponse<Service>> CreateAsync(Service entity);
    Task<ServiceResponse<Service>> UpdateAsync(int id, Service entity);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}