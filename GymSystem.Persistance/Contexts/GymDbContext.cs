using GymSystem.Domain.Entities;
using GymSystem.Persistance.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GymSystem.Persistance.Contexts;

public class GymDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public GymDbContext(DbContextOptions<GymDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Identity tables için gerekli

        modelBuilder.HasDefaultSchema("public");

        // Tüm mapping'leri otomatik yükle (Mappings klasöründeki IEntityTypeConfiguration'lar)
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // AppUser - Member ilişkisi
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasOne(u => u.Member)
                .WithOne()
                .HasForeignKey<AppUser>(u => u.MemberId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(u => u.GymLocation)
                .WithMany()
                .HasForeignKey(u => u.GymLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed Data
        DatabaseSeeder.SeedData(modelBuilder);
        IdentitySeeder.SeedIdentityData(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL için DateTime'ı timestamp without time zone olarak kullan
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp without time zone");
            
        configurationBuilder.Properties<DateTime?>()
            .HaveColumnType("timestamp without time zone");
    }
}