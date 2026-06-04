using Repository.Enums.Behaviors;

namespace Repository.Repositories.Generic;

/// <summary>
/// General repository implementation that contains basic CRUD operations on the database
/// </summary>
/// <typeparam name="T">The class model for the repository</typeparam>
public interface IBaseRepository<T> where T : class
{
    /// <summary>
    /// Read the entity with a specific key
    /// </summary>
    /// <param name="id">The key of the entity</param>
    /// <param name="behavior">Behavior describing what joins the query should include</param>
    /// <param name="includes">Specific includes to be used</param>
    /// <returns>A task with the final result of the query</returns>
    Task<T?> GetByIdAsync(ulong id, IncludeBehavior behavior, Func<IQueryable<T>, IQueryable<T>>? includes = null);

    /// <summary>
    /// Read all entities from the table (WARNING: only use in testing with few entities in a table)
    /// </summary>
    /// <param name="behavior">Behavior describing what joins the query should include</param>
    /// <param name="includes">Specific includes to be used</param>
    /// <returns>A task with the final result of the query</returns>
    Task<IEnumerable<T>> GetAllAsync(IncludeBehavior behavior, Func<IQueryable<T>, IQueryable<T>>? includes = null);

    /// <summary>
    /// Create an entity in the table
    /// </summary>
    /// <param name="entity">Entity model to be used for the creation</param>
    /// <returns>A task for the creation process</returns>
    Task AddAsync(T entity);

    /// <summary>
    /// Update an entity in the table
    /// </summary>
    /// <param name="entity">Entity model to be used for the update.
    /// It uses the primary key to find the entity in the table</param>
    void Update(T entity);

    /// <summary>
    /// Delete an entity in the table
    /// </summary>
    /// <param name="entity">Entity model to be used for the deletion.
    /// It uses the primary key to find the entity in the table</param>
    void Delete(T entity);

    /// <summary>
    /// Saves all changes made in the repository
    /// </summary>
    /// <returns>A task for the saving process</returns>
    Task SaveAsync();
}
