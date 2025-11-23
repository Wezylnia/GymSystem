using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class MemberViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
    public int? CurrentGymLocationId { get; set; }
    public string? CurrentGymLocationName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Helper properties
    public bool HasActiveMembership => MembershipEndDate.HasValue && MembershipEndDate.Value > DateTime.Now;
    public int? DaysRemaining => HasActiveMembership 
        ? (MembershipEndDate!.Value - DateTime.Now).Days 
        : null;
    public string MembershipStatus => HasActiveMembership 
        ? "Aktif Üyelik" 
        : (MembershipEndDate.HasValue ? "Süresi Dolmuş" : "Üyelik Yok");
}

public class CreateMemberViewModel
{
    [Required(ErrorMessage = "Ad zorunludur")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Soyad zorunludur")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
    [StringLength(200, ErrorMessage = "Email en fazla 200 karakter olabilir")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
    
    [Required(ErrorMessage = "Üyelik başlangıç tarihi zorunludur")]
    [DataType(DataType.Date)]
    [Display(Name = "Üyelik Başlangıç Tarihi")]
    public DateTime MembershipStartDate { get; set; } = DateTime.Now;
    
    [DataType(DataType.Date)]
    [Display(Name = "Üyelik Bitiş Tarihi")]
    public DateTime? MembershipEndDate { get; set; }
}

public class EditMemberViewModel : IValidatableObject
{
    public int Id { get; set; }
    
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
    
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [RegularExpression(@"^(05)[0-9]{9}$", ErrorMessage = "Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır (örn: 05XXXXXXXXX)")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "Telefon numarası tam 11 haneli olmalıdır")]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
    
    [DataType(DataType.Date)]
    [Display(Name = "Üyelik Başlangıç Tarihi")]
    public DateTime MembershipStartDate { get; set; }
    
    [DataType(DataType.Date)]
    [Display(Name = "Üyelik Bitiş Tarihi")]
    public DateTime? MembershipEndDate { get; set; }
    
    [Display(Name = "Aktif")]
    public bool IsActive { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Email domain kontrolü
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

        // Telefon numarası ek kontrol (opsiyonel alan)
        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            var cleanPhone = System.Text.RegularExpressions.Regex.Replace(PhoneNumber, @"[^0-9]", "");
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

        // Üyelik tarihleri kontrolü
        if (MembershipEndDate.HasValue && MembershipEndDate.Value <= MembershipStartDate)
        {
            yield return new ValidationResult(
                "Üyelik bitiş tarihi, başlangıç tarihinden sonra olmalıdır",
                new[] { nameof(MembershipEndDate) });
        }

        // Başlangıç tarihi gelecekte olamaz
        if (MembershipStartDate > DateTime.Now.Date)
        {
            yield return new ValidationResult(
                "Üyelik başlangıç tarihi gelecekte olamaz",
                new[] { nameof(MembershipStartDate) });
        }

        // Bitiş tarihi geçmişte olamaz (eğer set edilmişse)
        if (MembershipEndDate.HasValue && MembershipEndDate.Value < DateTime.Now.Date)
        {
            yield return new ValidationResult(
                "Üyelik bitiş tarihi geçmişte olamaz. Geçmişte biten bir üyelik için tarihi boş bırakınız.",
                new[] { nameof(MembershipEndDate) });
        }
    }
}
