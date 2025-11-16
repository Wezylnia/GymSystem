using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class GymLocationViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Salon adı zorunludur")]
    [StringLength(200, ErrorMessage = "Salon adı en fazla 200 karakter olabilir")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Adres zorunludur")]
    [StringLength(500, ErrorMessage = "Adres en fazla 500 karakter olabilir")]
    public string Address { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Şehir zorunludur")]
    [StringLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir")]
    public string City { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
    
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(200)]
    public string? Email { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
