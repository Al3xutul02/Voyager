using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Generic;

namespace Repository.Repositories.Abstractions;

/// <summary>
/// Server repository abstraction for querying and persisting Discord server
/// (<see cref="Server"/>) entities in the database.
/// </summary>
public interface IServerRepository : IBaseRepository<ulong, Server>
{
    /// <summary>
    /// Retrieves a server by its exact name.
    /// </summary>
    /// <param name="name">The name of the server to search for.</param>
    /// <param name="behavior">Specifies how related navigation properties should be included.</param>
    /// <param name="includes">Optional custom include query function when <paramref name="behavior"/> is <see cref="IncludeBehavior.GivenIncludes"/>.</param>
    /// <returns>The matching <see cref="Server"/> entity, or <c>null</c> if not found.</returns>
    public Task<Server?> GetByNameAsync(string name, IncludeBehavior behavior, Func<IQueryable<Server>, IQueryable<Server>>? includes = null);
}
