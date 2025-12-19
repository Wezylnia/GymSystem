using GymSystem.Application.Abstractions.Services.ITrainerService.Contract;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services.ITrainerService;

/// <summary>
/// Trainer service interface - DTO kullanımı ile
/// </summary>
public interface ITrainerService : IApplicationService
{
    Task<ServiceResponse<List<TrainerDto>>> GetAllAsync();
    Task<ServiceResponse<TrainerDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<TrainerDto>> CreateAsync(TrainerDto dto);
    Task<ServiceResponse<TrainerDto>> UpdateAsync(int id, TrainerDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}