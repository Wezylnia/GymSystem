using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class MemberMapping : IEntityTypeConfiguration<Member> {
    public void Configure(EntityTypeBuilder<Member> entity) {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // Member-specific properties
        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
        entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        entity.Property(e => e.MembershipStartDate).HasColumnType("timestamp without time zone");
        entity.Property(e => e.MembershipEndDate).HasColumnType("timestamp without time zone");

        // Aktif salon üyeliği ilişkisi
        entity.HasOne(m => m.CurrentGymLocation)
            .WithMany()
            .HasForeignKey(m => m.CurrentGymLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        entity.ToTable("members");
    }
}
