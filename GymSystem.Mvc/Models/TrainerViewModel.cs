using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class TrainerViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad 2-100 karakter arasında olmalıdır")]
    [RegularExpression(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$", ErrorMessage = "Ad sadece harf ve boşluk içermelidir")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad 2-100 karakter arasında olmalıdır")]
    [RegularExpression(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$", ErrorMessage = "Soyad sadece harf ve boşluk içermelidir")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(200, ErrorMessage = "Email en fazla 200 karakter olabilir")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Geçerli bir email formatı giriniz")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Telefon numarası 10-20 karakter arasında olmalıdır")]
    [RegularExpression(@"^[0-9\s\(\)\-\+]+$", ErrorMessage = "Telefon numarası sadece rakam ve telefon karakterleri içerebilir")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
    
    [StringLength(2000, ErrorMessage = "Biyografi en fazla 2000 karakter olabilir")]
    public string? Bio { get; set; }
    
    [StringLength(500)]
    public string? PhotoUrl { get; set; }
    
    [Required(ErrorMessage = "Spor salonu seçimi zorunludur")]
    public int GymLocationId { get; set; }
    
    public string? GymLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
