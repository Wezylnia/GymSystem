using GymSystem.Domain.Enums;

namespace GymSystem.Domain.Entities;

public class Member : BaseEntity {
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; } = Gender.Male; // Cinsiyet bilgisi
    public DateTime? MembershipStartDate { get; set; } // Nullable - sadece üyelik onayında atanır
    public DateTime? MembershipEndDate { get; set; }

    // Aktif üyelik bilgisi
    public int? CurrentGymLocationId { get; set; } // Şu anda üye olunan salon

    // Navigation properties
    public GymLocation? CurrentGymLocation { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<AIWorkoutPlan> WorkoutPlans { get; set; } = new List<AIWorkoutPlan>();

    // Helper methods
    /// <summary>
    /// Üyenin aktif bir üyeliği olup olmadığını kontrol eder
    /// </summary>
    public bool HasActiveMembership() {
        return MembershipEndDate.HasValue && MembershipEndDate.Value > DateTime.Now;
    }

    /// <summary>
    /// Üyeliğin kaç gün sonra biteceğini hesaplar
    /// </summary>
    public int? DaysUntilMembershipExpires() {
        if (!MembershipEndDate.HasValue) return null;
        var days = (MembershipEndDate.Value - DateTime.Now).Days;
        return days > 0 ? days : 0;
    }
}