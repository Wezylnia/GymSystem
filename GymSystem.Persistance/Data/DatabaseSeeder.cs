using GymSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymSystem.Persistance.Data;

public static class DatabaseSeeder
{
    // Unspecified DateTime kullan (PostgreSQL timestamp without time zone için)
    private static readonly DateTime BaseDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);

    public static void SeedData(ModelBuilder modelBuilder)
    {
        SeedGymLocations(modelBuilder);
        SeedServices(modelBuilder);
        SeedTrainers(modelBuilder);
        SeedMembers(modelBuilder);
        SeedWorkingHours(modelBuilder);
        SeedTrainerAvailabilities(modelBuilder);
        SeedTrainerSpecialties(modelBuilder);
    }

    private static void SeedGymLocations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GymLocation>().HasData(
            new GymLocation
            {
                Id = 1,
                Name = "FitZone Merkez Şube",
                Address = "Atatürk Bulvarı No:123",
                City = "Sakarya",
                PhoneNumber = "05551234567",
                Email = "merkez@fitzone.com",
                Description = "Modern ekipmanlar ve profesyonel antrenörlerle hizmetinizdeyiz.",
                IsActive = true,
                CreatedAt = BaseDate
            },
            new GymLocation
            {
                Id = 2,
                Name = "FitZone Serdivan Şube",
                Address = "Gazi Caddesi No:456",
                City = "Sakarya",
                PhoneNumber = "05559876543",
                Email = "serdivan@fitzone.com",
                Description = "Geniş ve ferah alanımızda spora davetlisiniz.",
                IsActive = true,
                CreatedAt = BaseDate.AddMonths(3)
            }
        );
    }

    private static void SeedServices(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Service>().HasData(
            new Service { Id = 1, Name = "Fitness", Description = "Genel kondisyon ve kas geliştirme", DurationMinutes = 60, Price = 150.00m, GymLocationId = 1, IsActive = true, CreatedAt = BaseDate },
            new Service { Id = 2, Name = "Yoga", Description = "Esneklik ve zihin-beden dengesi", DurationMinutes = 45, Price = 100.00m, GymLocationId = 1, IsActive = true, CreatedAt = BaseDate },
            new Service { Id = 3, Name = "Pilates", Description = "Postür düzeltme ve core kuvvetlendirme", DurationMinutes = 50, Price = 120.00m, GymLocationId = 1, IsActive = true, CreatedAt = BaseDate },
            new Service { Id = 4, Name = "Cardio", Description = "Kalp sağlığı ve dayanıklılık", DurationMinutes = 45, Price = 80.00m, GymLocationId = 2, IsActive = true, CreatedAt = BaseDate.AddMonths(3) },
            new Service { Id = 5, Name = "Zumba", Description = "Eğlenceli dans ve fitness kombinasyonu", DurationMinutes = 60, Price = 90.00m, GymLocationId = 2, IsActive = true, CreatedAt = BaseDate.AddMonths(3) }
        );
    }

    private static void SeedTrainers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trainer>().HasData(
            new Trainer { Id = 1, FirstName = "Ahmet", LastName = "Yılmaz", Email = "ahmet.yilmaz@fitzone.com", PhoneNumber = "05551111111", Bio = "10 yıllık fitness antrenörlüğü deneyimi.", GymLocationId = 1, IsActive = true, CreatedAt = BaseDate },
            new Trainer { Id = 2, FirstName = "Elif", LastName = "Demir", Email = "elif.demir@fitzone.com", PhoneNumber = "05552222222", Bio = "Yoga ve pilates uzmanı.", GymLocationId = 1, IsActive = true, CreatedAt = BaseDate },
            new Trainer { Id = 3, FirstName = "Mehmet", LastName = "Kaya", Email = "mehmet.kaya@fitzone.com", PhoneNumber = "05553333333", Bio = "Cardio ve HIIT antrenörü.", GymLocationId = 2, IsActive = true, CreatedAt = BaseDate.AddMonths(3) }
        );
    }

    private static void SeedMembers(ModelBuilder modelBuilder)
    {
        var memberDate = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Unspecified);
        modelBuilder.Entity<Member>().HasData(
            new Member { Id = 1, FirstName = "Ayşe", LastName = "Şahin", Email = "ayse.sahin@example.com", PhoneNumber = "05554444444", MembershipStartDate = memberDate, MembershipEndDate = memberDate.AddYears(1), IsActive = true, CreatedAt = memberDate },
            new Member { Id = 2, FirstName = "Can", LastName = "Öztürk", Email = "can.ozturk@example.com", PhoneNumber = "05555555555", MembershipStartDate = memberDate.AddMonths(1), MembershipEndDate = memberDate.AddMonths(13), IsActive = true, CreatedAt = memberDate.AddMonths(1) },
            new Member { Id = 3, FirstName = "Zeynep", LastName = "Arslan", Email = "zeynep.arslan@example.com", PhoneNumber = "05556666666", MembershipStartDate = memberDate.AddMonths(-1), MembershipEndDate = memberDate.AddMonths(11), IsActive = true, CreatedAt = memberDate.AddMonths(-1) }
        );
    }

    private static void SeedWorkingHours(ModelBuilder modelBuilder)
    {
        var workingHours = new List<WorkingHours>();
        
        for (int i = 1; i <= 5; i++)
        {
            workingHours.Add(new WorkingHours { Id = i, GymLocationId = 1, DayOfWeek = (DayOfWeek)i, OpenTime = new TimeSpan(6, 0, 0), CloseTime = new TimeSpan(22, 0, 0), IsClosed = false, IsActive = true, CreatedAt = BaseDate });
        }
        workingHours.Add(new WorkingHours { Id = 6, GymLocationId = 1, DayOfWeek = DayOfWeek.Saturday, OpenTime = new TimeSpan(8, 0, 0), CloseTime = new TimeSpan(20, 0, 0), IsClosed = false, IsActive = true, CreatedAt = BaseDate });
        workingHours.Add(new WorkingHours { Id = 7, GymLocationId = 1, DayOfWeek = DayOfWeek.Sunday, OpenTime = new TimeSpan(0, 0, 0), CloseTime = new TimeSpan(0, 0, 0), IsClosed = true, IsActive = true, CreatedAt = BaseDate });

        for (int i = 1; i <= 5; i++)
        {
            workingHours.Add(new WorkingHours { Id = 7 + i, GymLocationId = 2, DayOfWeek = (DayOfWeek)i, OpenTime = new TimeSpan(7, 0, 0), CloseTime = new TimeSpan(21, 0, 0), IsClosed = false, IsActive = true, CreatedAt = BaseDate.AddMonths(3) });
        }
        workingHours.Add(new WorkingHours { Id = 13, GymLocationId = 2, DayOfWeek = DayOfWeek.Saturday, OpenTime = new TimeSpan(9, 0, 0), CloseTime = new TimeSpan(18, 0, 0), IsClosed = false, IsActive = true, CreatedAt = BaseDate.AddMonths(3) });
        workingHours.Add(new WorkingHours { Id = 14, GymLocationId = 2, DayOfWeek = DayOfWeek.Sunday, OpenTime = new TimeSpan(9, 0, 0), CloseTime = new TimeSpan(18, 0, 0), IsClosed = false, IsActive = true, CreatedAt = BaseDate.AddMonths(3) });

        modelBuilder.Entity<WorkingHours>().HasData(workingHours);
    }

    private static void SeedTrainerAvailabilities(ModelBuilder modelBuilder)
    {
        var availabilities = new List<TrainerAvailability>();
        int id = 1;

        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
        {
            availabilities.Add(new TrainerAvailability { Id = id++, TrainerId = 1, DayOfWeek = day, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0), IsActive = true, CreatedAt = BaseDate });
        }

        foreach (var day in new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday })
        {
            availabilities.Add(new TrainerAvailability { Id = id++, TrainerId = 2, DayOfWeek = day, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(18, 0, 0), IsActive = true, CreatedAt = BaseDate });
        }

        for (int i = 1; i <= 5; i++)
        {
            availabilities.Add(new TrainerAvailability { Id = id++, TrainerId = 3, DayOfWeek = (DayOfWeek)i, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), IsActive = true, CreatedAt = BaseDate.AddMonths(3) });
        }

        modelBuilder.Entity<TrainerAvailability>().HasData(availabilities);
    }

    private static void SeedTrainerSpecialties(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrainerSpecialty>().HasData(
            new TrainerSpecialty { Id = 1, TrainerId = 1, ServiceId = 1, ExperienceYears = 10, CertificateName = "ACE Personal Trainer", IsActive = true, CreatedAt = BaseDate },
            new TrainerSpecialty { Id = 2, TrainerId = 2, ServiceId = 2, ExperienceYears = 8, CertificateName = "RYT 200 Yoga Alliance", IsActive = true, CreatedAt = BaseDate },
            new TrainerSpecialty { Id = 3, TrainerId = 2, ServiceId = 3, ExperienceYears = 6, CertificateName = "STOTT Pilates", IsActive = true, CreatedAt = BaseDate },
            new TrainerSpecialty { Id = 4, TrainerId = 3, ServiceId = 4, ExperienceYears = 5, CertificateName = "ACSM Certified", IsActive = true, CreatedAt = BaseDate.AddMonths(3) },
            new TrainerSpecialty { Id = 5, TrainerId = 3, ServiceId = 5, ExperienceYears = 4, CertificateName = "Zumba Instructor", IsActive = true, CreatedAt = BaseDate.AddMonths(3) }
        );
    }
}