using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class AppointmentMapping : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> entity)
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

        // Appointment-specific properties
        entity.Property(e => e.AppointmentDate).HasColumnType("timestamp without time zone").IsRequired();
        entity.Property(e => e.DurationMinutes).IsRequired();
        entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
        entity.Property(e => e.Status).IsRequired();
        entity.Property(e => e.Notes).HasMaxLength(1000);

        // Relationships
        entity.HasOne(e => e.Member)
            .WithMany(m => m.Appointments)
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Trainer)
            .WithMany(t => t.Appointments)
            .HasForeignKey(e => e.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Service)
            .WithMany(s => s.Appointments)
            .HasForeignKey(e => e.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable("appointments");
    }
}
