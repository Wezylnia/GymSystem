using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class TrainerSpecialtyMapping : IEntityTypeConfiguration<TrainerSpecialty> {
    public void Configure(EntityTypeBuilder<TrainerSpecialty> entity) {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // TrainerSpecialty-specific properties
        entity.Property(e => e.ExperienceYears).IsRequired();
        entity.Property(e => e.CertificateName).HasMaxLength(200);

        // Relationships
        entity.HasOne(e => e.Trainer)
            .WithMany(t => t.Specialties)
            .HasForeignKey(e => e.TrainerId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Service)
            .WithMany(s => s.TrainerSpecialties)
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("trainer_specialties");
    }
}
