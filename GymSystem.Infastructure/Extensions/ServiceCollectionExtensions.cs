using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Factory.Utility;
using GymSystem.Common.Services;
using GymSystem.Persistance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymSystem.Infastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsFileName = "appsettings.json")
    {
        // 1. Persistence layer'ı kaydet (DbContext + Identity)
        services.AddPersistenceInfrastructure(configuration, settingsFileName);

        // 2. Generic Factory'leri kaydet (open generic types)
        services.AddScoped(typeof(UtilityFactory<>), typeof(ConcreteUtilityFactory<>));
        services.AddScoped(typeof(BaseFactory<>), typeof(BaseConcreteFactory<>));

        // 3. Generic CRUD Service'i kaydet (open generic type)
        services.AddScoped(typeof(IGenericCrudService<>), typeof(GenericCrudService<>));

        // NOT: Application servisleri API/MVC Program.cs'de manuel olarak kaydedilecek
        // Circular dependency nedeniyle burada kaydedemiyoruz

        return services;
    }
}