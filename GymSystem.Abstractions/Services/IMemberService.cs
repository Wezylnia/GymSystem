using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface IMemberService : IApplicationService {
    Task<ServiceResponse<List<Member>>> GetAllAsync();
    Task<ServiceResponse<Member?>> GetByIdAsync(int id);
    Task<ServiceResponse<Member>> CreateAsync(Member entity);
    Task<ServiceResponse<Member>> UpdateAsync(int id, Member entity);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
    Task<ServiceResponse<IEnumerable<Member>>> GetAllMembersWithGymLocationAsync();
}