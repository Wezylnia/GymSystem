namespace GymSystem.Domain.Entities;

/// <summary>
/// Tüm entity'ler için base class
/// Ortak alanlar: Id, CreatedAt, UpdatedAt, IsActive
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}