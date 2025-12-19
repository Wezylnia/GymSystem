using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class AIWorkoutPlanViewModel {
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

    // Member info
    public string MemberName { get; set; } = string.Empty;
}

public class CreateAIWorkoutPlanViewModel {
    public int MemberId { get; set; }

    [Required(ErrorMessage = "Boy alanı zorunludur")]
    [Range(100, 250, ErrorMessage = "Boy 100-250 cm arasında olmalıdır")]
    [Display(Name = "Boy (cm)")]
    public decimal Height { get; set; }

    [Required(ErrorMessage = "Kilo alanı zorunludur")]
    [Range(30, 300, ErrorMessage = "Kilo 30-300 kg arasında olmalıdır")]
    [Display(Name = "Kilo (kg)")]
    public decimal Weight { get; set; }

    [Display(Name = "Vücut Tipi")]
    public string? BodyType { get; set; }

    [Required(ErrorMessage = "Hedef alanı zorunludur")]
    [Display(Name = "Hedef")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Hedef 10-500 karakter arasında olmalıdır")]
    public string Goal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Plan tipi seçiniz")]
    [Display(Name = "Plan Tipi")]
    public string PlanType { get; set; } = "Workout"; // Workout veya Diet

    [Display(Name = "Fotoğraf (Opsiyonel)")]
    public string? PhotoBase64 { get; set; }
}
