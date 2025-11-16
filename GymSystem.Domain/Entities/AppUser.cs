using Microsoft.AspNetCore.Identity;

namespace GymSystem.Domain.Entities;

/// <summary>
/// Uygulama kullanıcısı - IdentityUser'dan türetilmiş
/// Member tablosu ile 1-1 ilişki (opsiyonel)
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
    /// Navigation property
    /// </summary>
    public Member? Member { get; set; }
    
    /// <summary>
    /// Kayıt tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}
