using GymSystem.Application.Abstractions.Contract.Member;
using GymSystem.Common.Models;
using GymSystem.Common.ServiceRegistration;

namespace GymSystem.Application.Abstractions.Services;

public interface IMemberService : IApplicationService {
    Task<ServiceResponse<List<MemberDto>>> GetAllAsync();
    Task<ServiceResponse<MemberDto?>> GetByIdAsync(int id);
    Task<ServiceResponse<MemberDto>> CreateAsync(MemberDto dto);
    Task<ServiceResponse<MemberDto>> UpdateAsync(int id, MemberDto dto);
    Task<ServiceResponse<bool>> DeleteAsync(int id);
    Task<ServiceResponse<List<MemberDto>>> GetAllMembersWithGymLocationAsync();
}