using GymSystem.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Factory.Utility;

public abstract class UtilityFactory<T>
{
    public abstract ILogger<T> CreateLogger();
    public abstract IConfiguration CreateConfiguration();
    public abstract IServiceResponseHelper CreateServiceResponseHelper();
}
