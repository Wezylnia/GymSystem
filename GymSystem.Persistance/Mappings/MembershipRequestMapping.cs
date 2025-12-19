using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymSystem.Persistance.Mappings;

public class MembershipRequestMapping : IEntityTypeConfiguration<MembershipRequest> {
    public void Configure(EntityTypeBuilder<MembershipRequest> builder) {
        builder.ToTable("MembershipRequests");

        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.MemberId)
            .IsRequired();

        builder.Property(mr => mr.GymLocationId)
            .IsRequired();

        builder.Property(mr => mr.Duration)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(mr => mr.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(mr => mr.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(MembershipRequestStatus.Pending);

        builder.Property(mr => mr.Notes)
            .HasMaxLength(1000);

        builder.Property(mr => mr.AdminNotes)
            .HasMaxLength(1000);

        // İlişkiler
        builder.HasOne(mr => mr.Member)
            .WithMany()
            .HasForeignKey(mr => mr.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mr => mr.GymLocation)
            .WithMany()
            .HasForeignKey(mr => mr.GymLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index'ler
        builder.HasIndex(mr => mr.MemberId);
        builder.HasIndex(mr => mr.GymLocationId);
        builder.HasIndex(mr => mr.Status);
        builder.HasIndex(mr => mr.CreatedAt);
    }
}
