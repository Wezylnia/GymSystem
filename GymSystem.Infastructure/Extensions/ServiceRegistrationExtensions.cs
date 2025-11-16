using GymSystem.Common.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GymSystem.Infastructure.Extensions;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Belirtilen assembly'lerdeki tüm IApplicationService interface'ini implement eden servisleri
    /// Scoped (per-request) lifetime ile otomatik kaydeder.
    /// Her HTTP request için yeni bir servis instance'ı oluşturulur ve request sonunda dispose edilir.
    /// </summary>
    public static IServiceCollection AddAutoRegisteredServices(
        this IServiceCollection services, 
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();

            foreach (var implementationType in types)
            {
                var interfaces = implementationType.GetInterfaces();

                // IApplicationService'i implement eden interface'leri bul
                var serviceInterfaces = interfaces
                    .Where(i => typeof(IApplicationService).IsAssignableFrom(i) 
                             && i != typeof(IApplicationService) 
                             && i != typeof(IService)
                             && i.IsInterface)
                    .ToList();

                // Her bir service interface için implementasyonu Scoped olarak kaydet
                foreach (var serviceInterface in serviceInterfaces)
                {
                    services.AddScoped(serviceInterface, implementationType);
                }
            }
        }

        return services;
    }
}
