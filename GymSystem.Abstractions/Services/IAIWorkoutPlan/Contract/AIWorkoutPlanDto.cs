using GymSystem.Domain.Enums;

namespace GymSystem.Application.Abstractions.Services.IAIWorkoutPlan.Contract;

/// <summary>
/// AI Workout Plan için tek DTO
/// Hem request hem response için kullanılır
/// </summary>
public class AIWorkoutPlanDto {
    // Identity
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? MemberName { get; set; }

    // Plan Details
    public string PlanType { get; set; } = string.Empty; // "Workout" veya "Diet"
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public Gender Gender { get; set; } // Cinsiyet bilgisi
    public string? BodyType { get; set; }
    public string Goal { get; set; } = string.Empty;

    // AI Generated Content
    public string? AIGeneratedPlan { get; set; }
    public string? AIModel { get; set; }
    public string? PhotoBase64 { get; set; } // Request için
    public string? ImageUrl { get; set; } // Response için

    // Metadata
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}