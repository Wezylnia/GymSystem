using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class AIWorkoutPlanMapping : IEntityTypeConfiguration<AIWorkoutPlan> {
    public void Configure(EntityTypeBuilder<AIWorkoutPlan> entity) {
        // Table name
        entity.ToTable("ai_workout_plans");

        // Primary key
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();

        // Base entity properties
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // AIWorkoutPlan-specific properties
        entity.Property(e => e.MemberId).IsRequired();

        entity.Property(e => e.PlanType)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(e => e.Height)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.Weight)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.BodyType)
            .HasMaxLength(50);

        entity.Property(e => e.Goal)
            .IsRequired()
            .HasMaxLength(500);

        entity.Property(e => e.AIGeneratedPlan)
            .IsRequired()
            .HasColumnType("text");

        entity.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        entity.Property(e => e.AIModel)
            .HasMaxLength(100);

        // Relationships
        entity.HasOne(e => e.Member)
            .WithMany(m => m.WorkoutPlans)
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        entity.HasIndex(e => e.MemberId)
            .HasDatabaseName("ix_ai_workout_plans_member_id");

        entity.HasIndex(e => e.PlanType)
            .HasDatabaseName("ix_ai_workout_plans_plan_type");

        entity.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("ix_ai_workout_plans_created_at");
    }
}
