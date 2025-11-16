using GymSystem.Persistance.Contexts;
using GymSystem.Persistance.Database;
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

        // İleride başka DbContext'ler eklenebilir
        // Örnek:
        // serviceCollection.AddDbContext<AnotherDbContext>((serviceProvider, options) =>
        // {
        //     options.ConfigureDatabase("AnotherDbContext", configuration["Data:Another:MigrationsAssembly"], settingsFileName);
        // });
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

        // İleride başka context'ler için configuration eklenebilir
        // if (contextName == "AnotherDbContext")
        // {
        //     var connectionStringManager = new ConnectionStringManager(
        //         connectionStringKey: "AnotherDbContext", 
        //         settingsFileName: settingsFileName);
        //     
        //     string connectionString = connectionStringManager.GetConnectionString();
        //     builder.UseSqlServer(connectionString, sqlOptions =>
        //     {
        //         if (!string.IsNullOrEmpty(migrationAssembly))
        //         {
        //             sqlOptions.MigrationsAssembly(migrationAssembly);
        //         }
        //     });
        // }
    }
}
