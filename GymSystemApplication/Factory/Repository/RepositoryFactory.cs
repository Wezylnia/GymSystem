using GymSystem.Common.Factory;
using GymSystem.Common.Repositories;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Factory.Repository;

public interface IRepositoryFactory : IFactory
{
    // Specific entity repositories
    IRepository<Member> CreateMemberRepository();
    
    // Generic repository - herhangi bir entity için
    IRepository<T> CreateRepository<T>() where T : class;
    
    // Yeni entity'ler için repository metotları buraya eklenecek
    // IRepository<Trainer> CreateTrainerRepository();
    // IRepository<Membership> CreateMembershipRepository();
}