using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class TrainerViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20)]
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
