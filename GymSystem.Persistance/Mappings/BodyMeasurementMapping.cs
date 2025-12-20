using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class BodyMeasurementMapping : IEntityTypeConfiguration<BodyMeasurement> {
    public void Configure(EntityTypeBuilder<BodyMeasurement> entity) {
        // Table name
        entity.ToTable("body_measurements");

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

        // BodyMeasurement-specific properties
        entity.Property(e => e.MemberId).IsRequired();

        entity.Property(e => e.MeasurementDate)
            .IsRequired()
            .HasColumnType("timestamp without time zone");

        entity.Property(e => e.Height)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.Weight)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        entity.Property(e => e.Note)
            .HasMaxLength(500);

        // Relationships
        entity.HasOne(e => e.Member)
            .WithMany(m => m.BodyMeasurements)
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        entity.HasIndex(e => e.MemberId)
            .HasDatabaseName("ix_body_measurements_member_id");

        entity.HasIndex(e => e.MeasurementDate)
            .HasDatabaseName("ix_body_measurements_measurement_date");

        entity.HasIndex(e => new { e.MemberId, e.MeasurementDate })
            .HasDatabaseName("ix_body_measurements_member_date");
    }
}
