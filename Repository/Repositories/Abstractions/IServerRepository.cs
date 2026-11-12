using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Generic;

namespace Repository.Repositories.Abstractions;

public interface IServerRepository : IBaseRepository<ulong, Server>
{
    public Task<Server?> GetByNameAsync(string name, IncludeBehavior behavior, Func<IQueryable<Server>, IQueryable<Server>>? includes = null);
}
