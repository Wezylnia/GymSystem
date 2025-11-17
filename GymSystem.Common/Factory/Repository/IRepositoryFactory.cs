using GymSystem.Common.Repositories;

namespace GymSystem.Common.Factory.Repository;

public interface IRepositoryFactory : IFactory
{
    /// <summary>
    /// Generic repository - herhangi bir entity için
    /// </summary>
    IRepository<T> CreateRepository<T>() where T : class;
}
