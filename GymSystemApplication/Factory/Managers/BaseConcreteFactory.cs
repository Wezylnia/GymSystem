using GymSystem.Application.Factory.Repository;
using GymSystem.Application.Factory.Utility;

namespace GymSystem.Application.Factory.Managers;

public class BaseConcreteFactory<T> : BaseFactory<T>
{
    public BaseConcreteFactory(UtilityFactory<T> utilityFactory, IRepositoryFactory repositoryFactory)
        : base(utilityFactory, repositoryFactory)
    {
    }

    public override UtilityFactory<T> CreateUtilityFactory()
    {
        return utilityFactory;
    }

    public override IRepositoryFactory CreateRepositoryFactory()
    {
        return repositoryFactory;
    }
}
