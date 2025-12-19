using GymSystem.Common.Factory.Repository;
using GymSystem.Common.Repositories;
using GymSystem.Persistance.Contexts;
using GymSystem.Persistance.Repositories;

namespace GymSystem.Persistance.Factory;

public class ConcreteRepositoryFactory : IRepositoryFactory {
    private readonly GymDbContext _context;

    public ConcreteRepositoryFactory(GymDbContext context) {
        _context = context;
    }

    public IRepository<T> CreateRepository<T>() where T : class {
        return new Repository<T>(_context);
    }
}
