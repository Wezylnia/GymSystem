using GymSystem.Domain.Entities;
using GymSystem.Persistance.Contexts;
using GymSystem.Persistance.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymSystem.Persistance;

public static class ServiceCollectionExtensions
{
    public static void AddPersistenceInfrastructure(
        this IServiceCollection serviceCollection, 
        IConfiguration configuration, 
        string settingsFileName)
    {
        // GymDbContext'i kaydet
        serviceCollection.AddDbContext<GymDbContext>((serviceProvider, options) =>
        {
            options.ConfigureDatabase("GymDbContext", configuration["Data:Gym:MigrationsAssembly"], settingsFileName);
        });

        // ASP.NET Core Identity
        serviceCollection.AddIdentity<AppUser, IdentityRole<int>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 3;
            options.Password.RequiredUniqueChars = 0;

            // User settings
            options.User.RequireUniqueEmail = true;

            // SignIn settings
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<GymDbContext>()
        .AddDefaultTokenProviders();
    }

    public static void ConfigureDatabase(
        this DbContextOptionsBuilder builder, 
        string contextName, 
        string? migrationAssembly, 
        string settingsFileName)
    {
        if (contextName == "GymDbContext")
        {
            var connectionStringManager = new ConnectionStringManager(
                connectionStringKey: "GymDbContext", 
                settingsFileName: settingsFileName);
            
            string connectionString = connectionStringManager.GetConnectionString();

            builder.UseNpgsql(connectionString, npgsqlOptions =>
            {
                if (!string.IsNullOrEmpty(migrationAssembly))
                {
                    npgsqlOptions.MigrationsAssembly(migrationAssembly);
                }
            });
        }
    }
}
