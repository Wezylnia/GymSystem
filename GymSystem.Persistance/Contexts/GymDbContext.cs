using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymSystem.Persistance.Contexts;

public class GymDbContext : DbContext
{
    public GymDbContext(DbContextOptions<GymDbContext> options)
        : base(options)
    {
    }

    public DbSet<Member> Members { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        // Member entity configuration
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            
            // PostgreSQL timestamp without time zone
            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp without time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp without time zone");
            
            entity.Property(e => e.MembershipStartDate)
                .HasColumnType("timestamp without time zone");
            
            entity.Property(e => e.MembershipEndDate)
                .HasColumnType("timestamp without time zone");
            
            entity.ToTable("members");
        });

        base.OnModelCreating(modelBuilder);
    }
}