using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Factory.Utility;

namespace GymSystem.Common.Factory.Managers;

public class BaseConcreteFactory<T> : BaseFactory<T> {
    public BaseConcreteFactory(
        UtilityFactory<T> utilityFactory,
        IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider)
        : base(utilityFactory, repositoryFactory, serviceProvider) {
    }

    public override UtilityFactory<T> CreateUtilityFactory() {
        return utilityFactory;
    }

    public override IRepositoryFactory CreateRepositoryFactory() {
        return repositoryFactory;
    }
}