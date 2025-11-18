using GymSystem.Application.Abstractions.Contract.GymLocation;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services;

public interface IGymLocationService : IApplicationService
{
    Task<ServiceResponse<List<GymLocationDto>>> GetAllAsync();
    Task<ServiceResponse<GymLocationDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<GymLocationDto>> CreateAsync(GymLocationDto dto);
    Task<ServiceResponse<GymLocationDto>> UpdateAsync(int id, GymLocationDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
}