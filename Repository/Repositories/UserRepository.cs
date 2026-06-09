using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using Repository.Repositories.Generic;

namespace Repository.Repositories;

/// <summary>
/// User repository implementation for all user information.
/// </summary>
/// <param name="context">The database context to use.</param>
public class UserRepository(DbContext context) : BaseRepository<User>(context, "Id"), IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByNameAsync(string name, IncludeBehavior behavior, Func<IQueryable<User>, IQueryable<User>>? includes = null)
    {
        IQueryable<User> query = new QueryBuilder<User>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(u => u.Name == name);
    }
}
