using AutoMapper;
using GymSystem.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GymSystem.Common.Factory.Utility;

public class ConcreteUtilityFactory<T> : UtilityFactory<T>
{
    protected readonly IServiceProvider _serviceProvider;

    public ConcreteUtilityFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override ILogger<T> CreateLogger()
    {
        return _serviceProvider.GetRequiredService<ILogger<T>>();
    }

    public override IConfiguration CreateConfiguration()
    {
        return _serviceProvider.GetRequiredService<IConfiguration>();
    }

    public override IServiceResponseHelper CreateServiceResponseHelper()
    {
        return _serviceProvider.GetRequiredService<IServiceResponseHelper>();
    }

    public override IMapper CreateMapper() {
        return _serviceProvider.GetRequiredService<IMapper>();
    }
}