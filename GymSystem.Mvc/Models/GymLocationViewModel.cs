using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class GymLocationViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Salon adı zorunludur")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Salon adı 3-200 karakter arasında olmalıdır")]
    [Display(Name = "Salon Adı")]
    [RegularExpression(@"^[a-zA-ZğüşöçıİĞÜŞÖÇ0-9\s\-\.]+$", ErrorMessage = "Salon adı sadece harf, rakam, boşluk, tire ve nokta içerebilir")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Adres zorunludur")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Adres 10-500 karakter arasında olmalıdır")]
    [Display(Name = "Adres")]
    public string Address { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Şehir zorunludur")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Şehir adı 2-100 karakter arasında olmalıdır")]
    [Display(Name = "Şehir")]
    [RegularExpression(@"^[a-zA-ZğüşöçıİĞÜŞÖÇ\s]+$", ErrorMessage = "Şehir adı sadece harf ve boşluk içerebilir")]
    public string City { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Telefon numarası 10-20 karakter arasında olmalıdır")]
    [Display(Name = "Telefon")]
    [RegularExpression(@"^[0-9\s\(\)\-\+]+$", ErrorMessage = "Telefon numarası sadece rakam ve telefon karakterleri içerebilir")]
    public string? PhoneNumber { get; set; }
    
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz (örn: salon@example.com)")]
    [StringLength(200, ErrorMessage = "Email adresi en fazla 200 karakter olabilir")]
    [Display(Name = "E-posta")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Geçerli bir email formatı giriniz")]
    public string? Email { get; set; }
    
    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
