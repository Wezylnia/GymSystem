using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using GymSystem.Domain.Enums;

namespace GymSystem.Mvc.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(200, ErrorMessage = "Email en fazla 200 karakter olabilir")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Şifre zorunludur")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;
    
    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; }
    
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad en az 2, en fazla 100 karakter olmalıdır")]
    [RegularExpression(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$", ErrorMessage = "Ad sadece harf ve boşluk içermelidir")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad en az 2, en fazla 100 karakter olmalıdır")]
    [RegularExpression(@"^[a-zA-ZğüşıöçĞÜŞİÖÇ\s]+$", ErrorMessage = "Soyad sadece harf ve boşluk içermelidir")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(200, ErrorMessage = "Email en fazla 200 karakter olabilir")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Email formatı geçersiz")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [RegularExpression(@"^(05)[0-9]{9}$", ErrorMessage = "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır (örn: 05XXXXXXXXX)")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Telefon numarası tam 11 haneli olmalıdır")]
    [Display(Name = "Telefon")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Cinsiyet seçimi zorunludur")]
    [Display(Name = "Cinsiyet")]
    public Gender Gender { get; set; } = Gender.Male;
    
    [Required(ErrorMessage = "Şifre zorunludur")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Şifre tekrarı zorunludur")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
    [Display(Name = "Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Email domain kontrolü (opsiyonel ekstra kontrol)
        if (!string.IsNullOrEmpty(Email))
        {
            var emailParts = Email.Split('@');
            if (emailParts.Length == 2)
            {
                var domain = emailParts[1];
                if (domain.Length < 3 || !domain.Contains('.'))
                {
                    yield return new ValidationResult(
                        "Email domain geçersiz görünüyor",
                        new[] { nameof(Email) });
                }
            }
        }

        // Telefon numarası ek kontrol
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            // Boşluk, tire gibi karakterleri temizle
            var cleanPhone = Regex.Replace(PhoneNumber, @"[^0-9]", "");
            if (cleanPhone.Length != 11 || !cleanPhone.StartsWith("05"))
            {
                yield return new ValidationResult(
                    "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır",
                    new[] { nameof(PhoneNumber) });
            }
        }

        // İsim ve soyad boşluk kontrolü
        if (!string.IsNullOrWhiteSpace(FirstName) && FirstName.Trim().Length < 2)
        {
            yield return new ValidationResult(
                "Ad en az 2 karakter olmalıdır",
                new[] { nameof(FirstName) });
        }

        if (!string.IsNullOrWhiteSpace(LastName) && LastName.Trim().Length < 2)
        {
            yield return new ValidationResult(
                "Soyad en az 2 karakter olmalıdır",
                new[] { nameof(LastName) });
        }
    }
}
