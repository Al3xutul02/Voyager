using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Repository.Repositories.Generic;

/// <summary>
/// The implementation of the <see cref="IBaseRepository{KeyType, T}"/> interface
/// </summary>
/// <typeparam name="KeyType">The type of the primary key (can be a single type or a ValueTuple for composite keys)</typeparam>
/// <typeparam name="T">The class model for the repository</typeparam>
/// <param name="context">The context of the database that the repository belongs to</param>
/// <param name="keyNames">The class primary key name(s)</param>
public abstract class BaseRepository<KeyType, T>(DbContext context, params string[] keyNames)
    : IBaseRepository<KeyType, T>
    where KeyType : notnull
    where T : class
{
    protected readonly DbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    protected readonly string[] _keyNames = keyNames;
    // Type cache for better performance
    protected static readonly IQueryable<PropertyInfo> _entityProperties = typeof(T).GetProperties().AsQueryable();

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(KeyType id, IncludeBehavior behavior, Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = new QueryBuilder<T>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(BuildKeyPredicate(id));
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync(IncludeBehavior behavior, Func<IQueryable<T>, IQueryable<T>>? includes = null)
    {
        IQueryable<T> query = new QueryBuilder<T>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();
        return await query.ToListAsync();
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    /// <inheritdoc />
    public virtual void Update(T entity) => _dbSet.Update(entity);

    /// <inheritdoc />
    public virtual void Delete(T entity) => _dbSet.Remove(entity);

    /// <inheritdoc />
    public virtual async Task SaveAsync() => await _context.SaveChangesAsync();

    private Expression<Func<T, bool>> BuildKeyPredicate(KeyType id)
    {
        var parameter = Expression.Parameter(typeof(T), "e");

        if (_keyNames.Length == 1 && id is not ITuple)
        {
            var property = Expression.Property(parameter, _keyNames[0]);
            var constant = Expression.Constant(id, property.Type);
            var equal = Expression.Equal(property, constant);
            return Expression.Lambda<Func<T, bool>>(equal, parameter);
        }

        if (id is ITuple tuple)
        {
            if (tuple.Length != _keyNames.Length)
                throw new InvalidOperationException($"Composite key length ({tuple.Length}) does not match configured key names ({_keyNames.Length}).");

            Expression? combined = null;
            for (int i = 0; i < _keyNames.Length; i++)
            {
                var property = Expression.Property(parameter, _keyNames[i]);
                var constant = Expression.Constant(tuple[i], property.Type);
                var equal = Expression.Equal(property, constant);
                combined = combined == null ? equal : Expression.AndAlso(combined, equal);
            }

            return Expression.Lambda<Func<T, bool>>(combined!, parameter);
        }

        throw new InvalidOperationException("Invalid key configuration.");
    }
}
