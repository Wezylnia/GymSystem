using Microsoft.AspNetCore.Identity;

namespace GymSystem.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısı - IdentityUser'dan türetilmiş
/// 3 Rol: Admin, GymOwner, Member
/// </summary>
public class AppUser : IdentityUser<int>
{
    /// <summary>
    /// Kullanıcının adı
    /// </summary>
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// Kullanıcının soyadı
    /// </summary>
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// Member ile ilişki (opsiyonel - sadece Member rolündekiler için)
    /// </summary>
    public int? MemberId { get; set; }
    
    /// <summary>
    /// GymLocation ile ilişki (opsiyonel - sadece GymOwner rolündekiler için)
    /// </summary>
    public int? GymLocationId { get; set; }
    
    /// <summary>
    /// Member navigation property
    /// </summary>
    public Member? Member { get; set; }
    
    /// <summary>
    /// GymLocation navigation property
    /// </summary>
    public GymLocation? GymLocation { get; set; }
    
    /// <summary>
    /// Kayıt tarihi - PostgreSQL uyumlu (Unspecified Kind)
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}
