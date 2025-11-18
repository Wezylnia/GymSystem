using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface ITrainerService : IApplicationService {
    Task<ServiceResponse<List<Trainer>>> GetAllAsync();
    Task<ServiceResponse<Trainer?>> GetByIdAsync(int id);
    Task<ServiceResponse<Trainer>> CreateAsync(Trainer entity);
    Task<ServiceResponse<Trainer>> UpdateAsync(int id, Trainer entity);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}