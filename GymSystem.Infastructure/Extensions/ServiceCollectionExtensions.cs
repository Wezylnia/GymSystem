using GymSystem.Persistance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GymSystem.Infastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsFileName = "appsettings.json")
    {
        // 1. Persistence layer'ı kaydet
        services.AddPersistenceInfrastructure(configuration, settingsFileName);

        // 2. Application assembly'yi yükle
        var applicationAssembly = Assembly.Load("GymSystem.Application");

        // 3. Generic Factory'leri manuel kaydet (open generic types - abstract class)
        // LifomatikCore pattern: abstract class olarak register ediyoruz
        var utilityFactoryBase = applicationAssembly.GetType("GymSystem.Application.Factory.Utility.UtilityFactory`1");
        var utilityFactoryImpl = applicationAssembly.GetType("GymSystem.Application.Factory.Utility.ConcreteUtilityFactory`1");
        var baseFactoryBase = applicationAssembly.GetType("GymSystem.Application.Factory.Managers.BaseFactory`1");
        var baseFactoryImpl = applicationAssembly.GetType("GymSystem.Application.Factory.Managers.BaseConcreteFactory`1");

        if (utilityFactoryBase != null && utilityFactoryImpl != null)
        {
            services.AddScoped(utilityFactoryBase, utilityFactoryImpl);
        }

        if (baseFactoryBase != null && baseFactoryImpl != null)
        {
            services.AddScoped(baseFactoryBase, baseFactoryImpl);
        }

        // 4. Generic CRUD Service'i manuel kaydet (open generic type)
        var genericCrudServiceInterface = applicationAssembly.GetType("GymSystem.Application.Abstractions.Services.IGenericCrudService`1");
        var genericCrudServiceImpl = applicationAssembly.GetType("GymSystem.Application.Services.Generic.GenericCrudService`1");

        if (genericCrudServiceInterface != null && genericCrudServiceImpl != null)
        {
            services.AddScoped(genericCrudServiceInterface, genericCrudServiceImpl);
        }

        // 5. Common assembly'deki NON-GENERIC servisleri otomatik kaydet
        var commonAssembly = Assembly.Load("GymSystem.Common");
        services.AddAutoRegisteredServices(commonAssembly);

        // 6. Application assembly'deki NON-GENERIC servisleri otomatik kaydet
        services.AddAutoRegisteredServices(applicationAssembly);

        return services;
    }
}