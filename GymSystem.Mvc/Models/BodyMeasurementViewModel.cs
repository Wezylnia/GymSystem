using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

/// <summary>
/// Body Measurement için View Model
/// </summary>
public class BodyMeasurementViewModel {
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string? MemberName { get; set; }

    [Display(Name = "Ölçüm Tarihi")]
    [Required(ErrorMessage = "Ölçüm tarihi zorunludur")]
    [DataType(DataType.Date)]
    public DateTime MeasurementDate { get; set; } = DateTime.Today;

    [Display(Name = "Boy (cm)")]
    [Required(ErrorMessage = "Boy zorunludur")]
    [Range(50, 250, ErrorMessage = "Boy 50-250 cm arasýnda olmalýdýr")]
    public decimal Height { get; set; }

    [Display(Name = "Kilo (kg)")]
    [Required(ErrorMessage = "Kilo zorunludur")]
    [Range(20, 300, ErrorMessage = "Kilo 20-300 kg arasýnda olmalýdýr")]
    public decimal Weight { get; set; }

    [Display(Name = "Not")]
    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir")]
    public string? Note { get; set; }

    /// <summary>
    /// Bir önceki ölçüme göre boy deðiþimi (cm)
    /// </summary>
    [Display(Name = "Boy Deðiþimi")]
    public decimal? HeightChange { get; set; }

    /// <summary>
    /// Bir önceki ölçüme göre kilo deðiþimi (kg)
    /// </summary>
    [Display(Name = "Kilo Deðiþimi")]
    public decimal? WeightChange { get; set; }

    /// <summary>
    /// BMI (Vücut Kitle Ýndeksi)
    /// </summary>
    [Display(Name = "BMI")]
    public decimal BMI => Height > 0 ? Math.Round(Weight / ((Height / 100) * (Height / 100)), 1) : 0;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// BMI Kategorisi
    /// </summary>
    public string BMICategory {
        get {
            var bmi = BMI;
            if (bmi < 18.5m) return "Zayýf";
            if (bmi < 25m) return "Normal";
            if (bmi < 30m) return "Fazla Kilolu";
            return "Obez";
        }
    }

    /// <summary>
    /// BMI Badge rengi
    /// </summary>
    public string BMIBadgeClass {
        get {
            var bmi = BMI;
            if (bmi < 18.5m) return "bg-warning";
            if (bmi < 25m) return "bg-success";
            if (bmi < 30m) return "bg-warning";
            return "bg-danger";
        }
    }
}

/// <summary>
/// Liste sayfasý için View Model
/// </summary>
public class BodyMeasurementListViewModel {
    public List<BodyMeasurementViewModel> Measurements { get; set; } = new();
    public int MemberId { get; set; }
    public string? MemberName { get; set; }
    
    // Ýstatistikler
    public decimal? CurrentWeight { get; set; }
    public decimal? CurrentHeight { get; set; }
    public decimal? TotalWeightChange { get; set; }
    public decimal? TotalHeightChange { get; set; }
    public int TotalMeasurements { get; set; }
}
