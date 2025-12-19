using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class GymLocationMapping : IEntityTypeConfiguration<GymLocation> {
    public void Configure(EntityTypeBuilder<GymLocation> entity) {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // GymLocation-specific properties
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
        entity.Property(e => e.City).IsRequired().HasMaxLength(100);
        entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        entity.Property(e => e.Email).HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(1000);

        entity.ToTable("gym_locations");
    }
}
