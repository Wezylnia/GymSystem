using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymSystem.Persistance.Data;

public static class IdentitySeeder {
    private static readonly DateTime IdentityDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

    public static void SeedIdentityData(ModelBuilder modelBuilder) {
        SeedRoles(modelBuilder);
        SeedUsers(modelBuilder);
        SeedUserRoles(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder) {
        modelBuilder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole<int> {
                Id = 2,
                Name = "GymOwner",
                NormalizedName = "GYMOWNER",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            },
            new IdentityRole<int> {
                Id = 3,
                Name = "Member",
                NormalizedName = "MEMBER",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            }
        );
    }

    private static void SeedUsers(ModelBuilder modelBuilder) {
        var hasher = new PasswordHasher<AppUser>();

        // 1. Admin user (sistem yöneticisi - her şeyi yapabilir)
        var adminUser = new AppUser {
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

        // 2. GymOwner user 1 (FitZone Merkez Şube sahibi)
        var gymOwner1 = new AppUser {
            Id = 2,
            UserName = "owner.merkez@fitzone.com",
            NormalizedUserName = "OWNER.MERKEZ@FITZONE.COM",
            Email = "owner.merkez@fitzone.com",
            NormalizedEmail = "OWNER.MERKEZ@FITZONE.COM",
            EmailConfirmed = true,
            FirstName = "Mehmet",
            LastName = "Yılmaz",
            GymLocationId = 1, // Merkez Şube
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumber = "05551234567",
            PhoneNumberConfirmed = true,
            CreatedAt = IdentityDate,
            IsActive = true
        };
        gymOwner1.PasswordHash = hasher.HashPassword(gymOwner1, "owner123");

        // 3. GymOwner user 2 (FitZone Serdivan Şube sahibi)
        var gymOwner2 = new AppUser {
            Id = 3,
            UserName = "owner.serdivan@fitzone.com",
            NormalizedUserName = "OWNER.SERDIVAN@FITZONE.COM",
            Email = "owner.serdivan@fitzone.com",
            NormalizedEmail = "OWNER.SERDIVAN@FITZONE.COM",
            EmailConfirmed = true,
            FirstName = "Fatma",
            LastName = "Kaya",
            GymLocationId = 2, // Serdivan Şube
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            PhoneNumber = "05559876543",
            PhoneNumberConfirmed = true,
            CreatedAt = IdentityDate.AddMonths(3),
            IsActive = true
        };
        gymOwner2.PasswordHash = hasher.HashPassword(gymOwner2, "owner123");

        // 4. Member user 1 (Ayşe Şahin - üye)
        var memberUser1 = new AppUser {
            Id = 4,
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
        memberUser1.PasswordHash = hasher.HashPassword(memberUser1, "member123");

        // 5. Member user 2 (Can Öztürk - üye)
        var memberUser2 = new AppUser {
            Id = 5,
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
        memberUser2.PasswordHash = hasher.HashPassword(memberUser2, "member123");

        modelBuilder.Entity<AppUser>().HasData(adminUser, gymOwner1, gymOwner2, memberUser1, memberUser2);
    }

    private static void SeedUserRoles(ModelBuilder modelBuilder) {
        modelBuilder.Entity<IdentityUserRole<int>>().HasData(
            // Admin user -> Admin role
            new IdentityUserRole<int> { UserId = 1, RoleId = 1 },

            // GymOwner users -> GymOwner role
            new IdentityUserRole<int> { UserId = 2, RoleId = 2 },
            new IdentityUserRole<int> { UserId = 3, RoleId = 2 },

            // Member users -> Member role
            new IdentityUserRole<int> { UserId = 4, RoleId = 3 },
            new IdentityUserRole<int> { UserId = 5, RoleId = 3 }
        );
    }
}
