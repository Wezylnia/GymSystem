using GymSystem.Application.Factory.Repository;
using GymSystem.Application.Factory.Utility;

namespace GymSystem.Application.Factory.Managers;

public abstract class BaseFactory<T>
{
    protected readonly UtilityFactory<T> utilityFactory;
    protected readonly IRepositoryFactory repositoryFactory;

    protected BaseFactory(UtilityFactory<T> utilityFactory, IRepositoryFactory repositoryFactory)
    {
        this.utilityFactory = utilityFactory;
        this.repositoryFactory = repositoryFactory;
    }

    public abstract UtilityFactory<T> CreateUtilityFactory();
    public abstract IRepositoryFactory CreateRepositoryFactory();
}
