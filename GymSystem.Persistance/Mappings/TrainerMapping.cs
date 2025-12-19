using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class TrainerMapping : IEntityTypeConfiguration<Trainer> {
    public void Configure(EntityTypeBuilder<Trainer> entity) {
        // Base entity configuration
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).UseIdentityAlwaysColumn();
        entity.Property(e => e.IsActive).HasDefaultValue(true);
        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamp without time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        // Trainer-specific properties
        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
        entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        entity.Property(e => e.Bio).HasMaxLength(2000);
        entity.Property(e => e.PhotoUrl).HasMaxLength(500);

        // Relationships
        entity.HasOne(e => e.GymLocation)
            .WithMany(g => g.Trainers)
            .HasForeignKey(e => e.GymLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.ToTable("trainers");
    }
}
