using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class ServiceMapping : IEntityTypeConfiguration<Service> {
    public void Configure(EntityTypeBuilder<Service> entity) {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // Service-specific properties
        entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        entity.Property(e => e.Description).HasMaxLength(1000);
        entity.Property(e => e.DurationMinutes).IsRequired();
        entity.Property(e => e.Price).HasColumnType("decimal(10,2)");

        // Relationships
        entity.HasOne(e => e.GymLocation)
            .WithMany(g => g.Services)
            .HasForeignKey(e => e.GymLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("services");
    }
}
