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
