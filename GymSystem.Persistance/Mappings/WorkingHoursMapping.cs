using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class WorkingHoursMapping : IEntityTypeConfiguration<WorkingHours> {
    public void Configure(EntityTypeBuilder<WorkingHours> entity) {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // WorkingHours-specific properties
        entity.Property(e => e.DayOfWeek).IsRequired();
        entity.Property(e => e.OpenTime).IsRequired();
        entity.Property(e => e.CloseTime).IsRequired();
        entity.Property(e => e.IsClosed).HasDefaultValue(false);

        // Relationships
        entity.HasOne(e => e.GymLocation)
            .WithMany(g => g.WorkingHours)
            .HasForeignKey(e => e.GymLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("working_hours");
    }
}
