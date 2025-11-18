using GymSystem.Common.Repositories;
using GymSystem.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GymSystem.Persistance.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly GymDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(GymDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// IQueryable döndürür - LINQ chain için (tracking enabled)
    /// </summary>
    public IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    /// <summary>
    /// IQueryable döndürür - Read-only queries için (no tracking, better performance)
    /// </summary>
    public IQueryable<T> QueryNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>>? predicate = null)
    {
        if (predicate == null)
        {
            return await _dbSet.ToListAsync();
        }
        
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return await Task.FromResult(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
        {
            return false;
        }

        _dbSet.Remove(entity);
        return true;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}