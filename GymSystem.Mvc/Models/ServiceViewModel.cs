using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class ServiceViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Hizmet adı zorunludur")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Hizmet adı 3-200 karakter arasında olmalıdır")]
    [Display(Name = "Hizmet Adı")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "Süre zorunludur")]
    [Range(1, 480, ErrorMessage = "Süre 1-480 dakika arasında olmalıdır")]
    public int DurationMinutes { get; set; }
    
    [Required(ErrorMessage = "Fiyat zorunludur")]
    [Range(0.01, 999999.99, ErrorMessage = "Fiyat 0.01-999999.99 arasında olmalıdır")]
    public decimal Price { get; set; }
    
    [Required(ErrorMessage = "Spor salonu seçimi zorunludur")]
    public int GymLocationId { get; set; }
    
    public string? GymLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
