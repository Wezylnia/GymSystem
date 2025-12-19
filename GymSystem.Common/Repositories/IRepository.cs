using System.Linq.Expressions;

namespace GymSystem.Common.Repositories;

public interface IRepository<T> where T : class {
    // Queryable - LINQ chain için
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();

    // Basic operations
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>>? predicate = null);
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    // Command operations
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<int> SaveChangesAsync();
}