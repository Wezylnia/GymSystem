using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface IAIWorkoutPlanService
{
    /// <summary>
    /// Kullanıcı bilgilerine göre AI workout planı oluşturur
    /// </summary>
    Task<AIWorkoutPlan> GenerateWorkoutPlanAsync(int memberId, decimal height, decimal weight, 
        string? bodyType, string goal, string? photoBase64 = null);

    /// <summary>
    /// Kullanıcı bilgilerine göre AI diet planı oluşturur
    /// </summary>
    Task<AIWorkoutPlan> GenerateDietPlanAsync(int memberId, decimal height, decimal weight, 
        string? bodyType, string goal, string? photoBase64 = null);

    /// <summary>
    /// Belirli bir üyenin tüm AI planlarını getirir
    /// </summary>
    Task<List<AIWorkoutPlan>> GetMemberPlansAsync(int memberId);

    /// <summary>
    /// ID'ye göre AI planını getirir
    /// </summary>
    Task<AIWorkoutPlan?> GetPlanByIdAsync(int id);

    /// <summary>
    /// AI planını siler
    /// </summary>
    Task<bool> DeletePlanAsync(int id);

    /// <summary>
    /// Tüm AI planlarını getirir (Admin için)
    /// </summary>
    Task<List<AIWorkoutPlan>> GetAllPlansAsync();
}
