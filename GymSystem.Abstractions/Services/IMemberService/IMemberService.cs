using GymSystem.Application.Abstractions.Services.IMemberService.Contract;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services.IMemberService;

public interface IMemberService : IApplicationService {
    Task<ServiceResponse<List<MemberDto>>> GetAllAsync();
    Task<ServiceResponse<MemberDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<MemberDto?>> GetByEmailAsync(string email);
    Task<ServiceResponse<MemberDto>> CreateAsync(MemberDto dto);
    Task<ServiceResponse<MemberDto>> UpdateAsync(int id, MemberDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
    Task<ServiceResponse<List<MemberDto>>> GetAllMembersWithGymLocationAsync();
    Task<ServiceResponse<List<MemberDto>>> GetMembersByGymLocationAsync(int gymLocationId);
}