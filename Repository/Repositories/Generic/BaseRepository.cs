using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using System.Reflection;

namespace Repository.Repositories.Generic;

/// <summary>
/// The implementation of the <see cref="IBaseRepository{T}"/> interface
/// </summary>
/// <typeparam name="T">The class model for the repository</typeparam>
/// <param name="context">The context of the database that the repository belongs to</param>
/// <param name="keyName">The class primary key name</param>
public abstract class BaseRepository<T>(DbContext context, string keyName)
    : IBaseRepository<T> where T : class
{
    protected readonly DbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    protected readonly string _keyName = keyName;
    // Type cache for better performance
    protected static readonly IQueryable<PropertyInfo> _entityProperties = typeof(T).GetProperties().AsQueryable();

    public virtual async Task<T?> GetByIdAsync(ulong id, IncludeBehavior behavior, Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = new QueryBuilder<T>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(e => EF.Property<ulong>(e, _keyName) == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(IncludeBehavior behavior, Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = new QueryBuilder<T>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();
        return await query.ToListAsync();
    }
    public virtual async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public virtual void Update(T entity) => _dbSet.Update(entity);

    public virtual void Delete(T entity) => _dbSet.Remove(entity);

    public virtual async Task SaveAsync() => await _context.SaveChangesAsync();
}