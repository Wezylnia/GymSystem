using GymSystem.Common.Repositories;
using GymSystem.Domain.Entities;
using GymSystem.Persistance.Contexts;
using GymSystem.Persistance.Repositories;

namespace GymSystem.Application.Factory.Repository;

public class ConcreteRepositoryFactory : IRepositoryFactory
{
    private readonly GymDbContext _context;

    public ConcreteRepositoryFactory(GymDbContext context)
    {
        _context = context;
    }

    public IRepository<Member> CreateMemberRepository()
    {
        return new Repository<Member>(_context);
    }

    // Generic repository factory method
    public IRepository<T> CreateRepository<T>() where T : class
    {
        return new Repository<T>(_context);
    }
    
    // Yeni entity repository'leri buraya eklenecek
    // public IRepository<Trainer> CreateTrainerRepository()
    // {
    //     return new Repository<Trainer>(_context);
    // }
}
