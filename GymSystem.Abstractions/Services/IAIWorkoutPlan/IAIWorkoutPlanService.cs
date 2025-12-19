using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan.Contract;
using GymSystem.Common.Models;

namespace GymSystem.Application.Abstractions.Services.IAIWorkoutPlan;

public interface IAIWorkoutPlanService {
    Task<ServiceResponse<AIWorkoutPlanDto>> GenerateWorkoutPlanAsync(AIWorkoutPlanDto request);
    Task<ServiceResponse<AIWorkoutPlanDto>> GenerateDietPlanAsync(AIWorkoutPlanDto request);
    Task<ServiceResponse<List<AIWorkoutPlanDto>>> GetMemberPlansAsync(int memberId);
    Task<ServiceResponse<AIWorkoutPlanDto?>> GetPlanByIdAsync(int id);
    Task<ServiceResponse<bool>> DeletePlanAsync(int id);
    Task<ServiceResponse<List<AIWorkoutPlanDto>>> GetAllPlansAsync();
}