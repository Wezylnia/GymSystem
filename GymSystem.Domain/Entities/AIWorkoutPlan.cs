using GymSystem.Domain.Enums;

namespace GymSystem.Domain.Entities;

/// <summary>
/// Yapay zeka tarafından oluşturulan egzersiz/diyet planları
/// </summary>
public class AIWorkoutPlan : BaseEntity {
    public int MemberId { get; set; }
    public string PlanType { get; set; } = string.Empty; // "Workout" veya "Diet"

    // Input verileri
    public decimal Height { get; set; } // Boy (cm)
    public decimal Weight { get; set; } // Kilo (kg)
    public Gender Gender { get; set; } // Cinsiyet
    public string? BodyType { get; set; } // Vücut tipi (Ectomorph, Mesomorph, Endomorph)
    public string Goal { get; set; } = string.Empty; // Hedef (kilo verme, kas yapma, vb.)

    // AI output
    public string AIGeneratedPlan { get; set; } = string.Empty; // AI'dan gelen plan metni
    public string? ImageUrl { get; set; } // DALL-E ile oluşturulan görsel URL'i
    public string? AIModel { get; set; } // Kullanılan AI modeli (örn: gpt-4, dall-e-3)

    // Navigation properties
    public Member Member { get; set; } = null!;
}
