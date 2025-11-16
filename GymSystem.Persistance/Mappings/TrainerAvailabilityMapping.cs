using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class TrainerAvailabilityMapping : IEntityTypeConfiguration<TrainerAvailability>
{
    public void Configure(EntityTypeBuilder<TrainerAvailability> entity)
    {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // TrainerAvailability-specific properties
        entity.Property(e => e.DayOfWeek).IsRequired();
        entity.Property(e => e.StartTime).IsRequired();
        entity.Property(e => e.EndTime).IsRequired();

        // Relationships
        entity.HasOne(e => e.Trainer)
            .WithMany(t => t.Availabilities)
            .HasForeignKey(e => e.TrainerId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("trainer_availabilities");
    }
}
