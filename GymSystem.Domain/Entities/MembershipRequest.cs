namespace GymSystem.Domain.Entities;

/// <summary>
/// Salon üyelik talepleri
/// Kullanıcılar salon seçip üyelik talebinde bulunur, admin/gym owner onaylar
/// </summary>
public class MembershipRequest : BaseEntity
{
    public int MemberId { get; set; }
    public int GymLocationId { get; set; }
    public MembershipDuration Duration { get; set; } // 1, 3, 6 aylık
    public decimal Price { get; set; } // Üyelik ücreti
    public MembershipRequestStatus Status { get; set; } = MembershipRequestStatus.Pending;
    public string? Notes { get; set; } // Kullanıcı notu
    public string? AdminNotes { get; set; } // Admin/GymOwner notu
    public int? ApprovedBy { get; set; } // Onaylayan kullanıcı (AppUser Id)
    public DateTime? ApprovedAt { get; set; } // Onay tarihi
    public DateTime? RejectedAt { get; set; } // Red tarihi

    // Navigation properties
    public Member Member { get; set; } = null!;
    public GymLocation GymLocation { get; set; } = null!;
}

/// <summary>
/// Üyelik süreleri
/// </summary>
public enum MembershipDuration
{
    OneMonth = 1,      // 1 aylık
    ThreeMonths = 3,   // 3 aylık
    SixMonths = 6      // 6 aylık
}

/// <summary>
/// Üyelik talebi durumları
/// </summary>
public enum MembershipRequestStatus
{
    Pending = 0,    // Onay bekliyor
    Approved = 1,   // Onaylandı
    Rejected = 2    // Reddedildi
}