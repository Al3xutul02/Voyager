using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using Repository.Repositories.Generic;

namespace Repository.Repositories;

public class ServerRepository(DbContext context) : BaseRepository<ulong, Server>(context, "Id"), IServerRepository
{
    /// <inheritdoc />
    public async Task<Server?> GetByNameAsync(string name, IncludeBehavior behavior, Func<IQueryable<Server>, IQueryable<Server>>? includes = null)
    {
        IQueryable<Server> query = new QueryBuilder<Server>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(s => s.Name == name);
    }
}
