using GymSystem.Common.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GymSystem.Infastructure.Extensions;

public static class ServiceRegistrationExtensions {
    /// <summary>
    /// Belirtilen assembly'lerdeki tüm IApplicationService interface'ini implement eden servisleri
    /// Scoped (per-request) lifetime ile otomatik kaydeder.
    /// Her HTTP request için yeni bir servis instance'ı oluşturulur ve request sonunda dispose edilir.
    /// </summary>
    public static IServiceCollection AddAutoRegisteredServices(
        this IServiceCollection services,
        params Assembly[] assemblies) {
        foreach (var assembly in assemblies) {
            Console.WriteLine($"[AutoRegister] Scanning assembly: {assembly.GetName().Name}");

            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();

            Console.WriteLine($"[AutoRegister] Found {types.Count} concrete classes");

            foreach (var implementationType in types) {
                var interfaces = implementationType.GetInterfaces();

                // IApplicationService'i implement eden interface'leri bul
                // DİKKAT: Interface'in kendisi IApplicationService'den türemiş olmalı
                var serviceInterfaces = interfaces
                    .Where(i => i.IsInterface &&
                               i != typeof(IApplicationService) &&
                               i != typeof(IService) &&
                               typeof(IApplicationService).IsAssignableFrom(i))
                    .ToList();

                // Her bir service interface için implementasyonu Scoped olarak kaydet
                foreach (var serviceInterface in serviceInterfaces) {
                    services.AddScoped(serviceInterface, implementationType);
                    Console.WriteLine($"[AutoRegister] ✓ Registered: {serviceInterface.Name} -> {implementationType.Name}");
                }
            }
        }

        return services;
    }
}
