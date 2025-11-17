using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Factory.Utility;

namespace GymSystem.Common.Factory.Managers;

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
