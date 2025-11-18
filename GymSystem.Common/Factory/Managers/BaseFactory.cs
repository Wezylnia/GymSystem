using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Factory.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace GymSystem.Common.Factory.Managers;

public abstract class BaseFactory<T>
{
    protected readonly UtilityFactory<T> utilityFactory;
    protected readonly IRepositoryFactory repositoryFactory;
    protected readonly IServiceProvider serviceProvider;

    protected BaseFactory(
        UtilityFactory<T> utilityFactory, 
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
    {
        this.utilityFactory = utilityFactory;
        this.repositoryFactory = repositoryFactory;
        this.serviceProvider = serviceProvider;
    }

    public abstract UtilityFactory<T> CreateUtilityFactory();
    public abstract IRepositoryFactory CreateRepositoryFactory();
    
    /// <summary>
    /// Diğer servislere erişim için - Dependency Injection
    /// </summary>
    public TService GetService<TService>() where TService : notnull
    {
        return serviceProvider.GetRequiredService<TService>();
    }
}