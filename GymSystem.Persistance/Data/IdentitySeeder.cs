using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymSystem.Persistance.Data;

public static class IdentitySeeder
{
    private static readonly DateTime IdentityDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

    public static void SeedIdentityData(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
        SeedUsers(modelBuilder);
        SeedUserRoles(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int>
            {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole<int>
            {
                Id = 2,
                Name = "Member",
                NormalizedName = "MEMBER",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        );
    }

    private static void SeedUsers(ModelBuilder modelBuilder)
    {
        var hasher = new PasswordHasher<AppUser>();

        // Admin user
        var adminUser = new AppUser
        {
            Id = 1,
            UserName = "admin@sakarya.edu.tr",
            NormalizedUserName = "ADMIN@SAKARYA.EDU.TR",
            Email = "admin@sakarya.edu.tr",
            NormalizedEmail = "ADMIN@SAKARYA.EDU.TR",
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumber = "05551234567",
            PhoneNumberConfirmed = true,
            CreatedAt = IdentityDate,
            IsActive = true
        };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "sau");

        // Member user 1 (Ayşe Şahin ile ilişkili)
        var memberUser1 = new AppUser
        {
            Id = 2,
            UserName = "ayse.sahin@example.com",
            NormalizedUserName = "AYSE.SAHIN@EXAMPLE.COM",
            Email = "ayse.sahin@example.com",
            NormalizedEmail = "AYSE.SAHIN@EXAMPLE.COM",
            EmailConfirmed = true,
            FirstName = "Ayşe",
            LastName = "Şahin",
            MemberId = 1,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumber = "05554444444",
            PhoneNumberConfirmed = true,
            CreatedAt = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        };
        memberUser1.PasswordHash = hasher.HashPassword(memberUser1, "123456");

        // Member user 2 (Can Öztürk ile ilişkili)
        var memberUser2 = new AppUser
        {
            Id = 3,
            UserName = "can.ozturk@example.com",
            NormalizedUserName = "CAN.OZTURK@EXAMPLE.COM",
            Email = "can.ozturk@example.com",
            NormalizedEmail = "CAN.OZTURK@EXAMPLE.COM",
            EmailConfirmed = true,
            FirstName = "Can",
            LastName = "Öztürk",
            MemberId = 2,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumber = "05555555555",
            PhoneNumberConfirmed = true,
            CreatedAt = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Unspecified),
            IsActive = true
        };
        memberUser2.PasswordHash = hasher.HashPassword(memberUser2, "123456");

        modelBuilder.Entity<AppUser>().HasData(adminUser, memberUser1, memberUser2);
    }

    private static void SeedUserRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUserRole<int>>().HasData(
            // Admin user -> Admin role
            new IdentityUserRole<int>
            {
                UserId = 1,
                RoleId = 1
            },
            // Member user 1 -> Member role
            new IdentityUserRole<int>
            {
                UserId = 2,
                RoleId = 2
            },
            // Member user 2 -> Member role
            new IdentityUserRole<int>
            {
                UserId = 3,
                RoleId = 2
            }
        );
    }
}
