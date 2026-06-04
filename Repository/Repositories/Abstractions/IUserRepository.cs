using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Generic;

namespace Repository.Repositories.Abstractions;

/// <summary>
/// User repository implementation for user information from the database
/// </summary>
public interface IUserRepository : IBaseRepository<User>
{
    /// <summary>
    /// Read the user with a specific name
    /// </summary>
    /// <param name="name">The name of the user</param>
    /// <param name="behavior">Behavior describing what joins the query should include</param>
    /// <param name="includes">Specific includes to be used</param>
    /// <returns>A task with the final result of the query</returns>
    public Task<User?> GetByNameAsync(string name, IncludeBehavior behavior, Func<IQueryable<User>, IQueryable<User>>? includes = null);
}
