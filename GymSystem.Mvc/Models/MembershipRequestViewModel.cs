using GymSystem.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace GymSystem.Mvc.Models;

public class MembershipRequestViewModel
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int GymLocationId { get; set; }
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public MembershipRequestStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? AdminNotes { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties için DTO'lar
    public MemberDto? Member { get; set; }
    public GymLocationDto? GymLocation { get; set; }

    // Display properties
    public string MemberName { get; set; } = string.Empty;
    public string GymLocationName { get; set; } = string.Empty;
    public string GymLocationAddress { get; set; } = string.Empty;
    
    public string StatusText => Status switch
    {
        MembershipRequestStatus.Pending => "Beklemede",
        MembershipRequestStatus.Approved => "Onaylandı",
        MembershipRequestStatus.Rejected => "Reddedildi",
        _ => "Bilinmiyor"
    };
    
    public string StatusCssClass => Status switch
    {
        MembershipRequestStatus.Pending => "warning",
        MembershipRequestStatus.Approved => "success",
        MembershipRequestStatus.Rejected => "danger",
        _ => "secondary"
    };
    
    public string DurationText => Duration switch
    {
        MembershipDuration.OneMonth => "1 Ay",
        MembershipDuration.ThreeMonths => "3 Ay",
        MembershipDuration.SixMonths => "6 Ay",
        _ => "Bilinmiyor"
    };
}

public class MemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class GymLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}

public class CreateMembershipRequestViewModel
{
    public int MemberId { get; set; }

    [Required(ErrorMessage = "Salon seçimi zorunludur")]
    [Display(Name = "Spor Salonu")]
    public int GymLocationId { get; set; }

    [Required(ErrorMessage = "Üyelik süresi seçimi zorunludur")]
    [Display(Name = "Üyelik Süresi")]
    public MembershipDuration Duration { get; set; }

    [Required(ErrorMessage = "Ücret alanı zorunludur")]
    [Range(0.01, 999999.99, ErrorMessage = "Ücret 0'dan büyük olmalıdır")]
    [Display(Name = "Ücret (TL)")]
    public decimal Price { get; set; }

    [Display(Name = "Notlar (Opsiyonel)")]
    [StringLength(1000, ErrorMessage = "Notlar en fazla 1000 karakter olabilir")]
    public string? Notes { get; set; }

    // For display purposes
    public List<GymLocationSelectItem> AvailableGyms { get; set; } = new();
}

public class GymLocationSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal OneMonthPrice { get; set; }
    public decimal ThreeMonthsPrice { get; set; }
    public decimal SixMonthsPrice { get; set; }
}

public class ManageMembershipRequestViewModel
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberEmail { get; set; } = string.Empty;
    public string? MemberPhone { get; set; }
    public int GymLocationId { get; set; }
    public string GymLocationName { get; set; } = string.Empty;
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public MembershipRequestStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    [Display(Name = "Yönetici Notu")]
    [StringLength(1000, ErrorMessage = "Not en fazla 1000 karakter olabilir")]
    public string? AdminNotes { get; set; }

    public string DurationText => Duration switch
    {
        MembershipDuration.OneMonth => "1 Ay",
        MembershipDuration.ThreeMonths => "3 Ay",
        MembershipDuration.SixMonths => "6 Ay",
        _ => "Bilinmiyor"
    };
}