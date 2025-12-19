namespace GymSystem.Mvc.Models.Dtos;

/// <summary>
/// API'den dönen AIWorkoutPlan DTO
/// </summary>
public class ApiAIWorkoutPlanDto {
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string? BodyType { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string AIGeneratedPlan { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? AIModel { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public ApiMemberDto? Member { get; set; }
}
