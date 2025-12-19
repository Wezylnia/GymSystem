using GymSystem.Application.Abstractions.Services.IGymLocationService.Contract;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services.IGymLocationService;

public interface IGymLocationService : IApplicationService {
    Task<ServiceResponse<List<GymLocationDto>>> GetAllAsync();
    Task<ServiceResponse<GymLocationDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<GymLocationDto>> CreateAsync(GymLocationDto dto);
    Task<ServiceResponse<GymLocationDto>> UpdateAsync(int id, GymLocationDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}