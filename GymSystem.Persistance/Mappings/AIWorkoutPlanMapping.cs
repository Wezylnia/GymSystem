using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class AIWorkoutPlanMapping : IEntityTypeConfiguration<AIWorkoutPlan>
{
    public void Configure(EntityTypeBuilder<AIWorkoutPlan> entity)
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

        // AIWorkoutPlan-specific properties
        entity.Property(e => e.PlanType).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Height).HasColumnType("decimal(5,2)");
        entity.Property(e => e.Weight).HasColumnType("decimal(5,2)");
        entity.Property(e => e.BodyType).HasMaxLength(50);
        entity.Property(e => e.Goal).IsRequired().HasMaxLength(500);
        entity.Property(e => e.AIGeneratedPlan).IsRequired();
        entity.Property(e => e.ImageUrl).HasMaxLength(500);
        entity.Property(e => e.AIModel).HasMaxLength(100);

        // Relationships
        entity.HasOne(e => e.Member)
            .WithMany(m => m.WorkoutPlans)
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("ai_workout_plans");
    }
}
