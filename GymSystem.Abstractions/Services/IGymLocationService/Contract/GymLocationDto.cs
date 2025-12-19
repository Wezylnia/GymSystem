using System.ComponentModel.DataAnnotations;

namespace GymSystem.Application.Abstractions.Services.IGymLocationService.Contract;

/// <summary>
/// GymLocation için tek DTO - Request ve Response için kullanılır
/// </summary>
public class GymLocationDto {
    public int Id { get; set; }

    [Required(ErrorMessage = "Salon adı zorunludur")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Salon adı 3-200 karakter arasında olmalıdır")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres zorunludur")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Adres 10-500 karakter arasında olmalıdır")]
    public string Address { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Description { get; set; }

    [Range(0, 10000, ErrorMessage = "Kapasite 0-10000 arasında olmalıdır")]
    public int Capacity { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}
