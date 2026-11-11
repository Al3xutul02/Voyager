using Repository.Enums.Behaviors;

namespace BusinessLogic.Services.Generic;

/// <summary>
/// General service implementation that contains basic CRUD operations on the database
/// </summary>
/// <typeparam name="T">The class model for the service</typeparam>
public interface IBaseService<KeyType, T, TReadModel, TCreateModel, TUpdateModel>
    where KeyType : notnull
    where T : class
    where TReadModel : class
    where TCreateModel : class
    where TUpdateModel : class
{
    /// <summary>
    /// Read the entity with a specific key
    /// </summary>
    /// <param name="id">The key of the entity</param>
    /// <returns>A task with the final result of the query</returns>
    Task<TReadModel?> GetByIdAsync(KeyType id);

    /// <summary>
    /// Read all entities from the table (WARNING: only use in testing with few entities in a table)
    /// </summary>
    /// <returns>A task with the final result of the query</returns>
    Task<IEnumerable<TReadModel>> GetAllAsync();

    /// <summary>
    /// Create an entity in the table
    /// </summary>
    /// <param name="entityCreateModel">Entity model to be used for the creation</param>
    /// <returns>A task with the action</returns>
    Task CreateAsync(TCreateModel entityCreateModel);

    /// <summary>
    /// Update an entity in the table
    /// </summary>
    /// <param name="entityUpdateModel">Entity model to be used for the update.
    /// It uses the primary key to find the entity in the table</param>
    /// <returns>A task with the action</returns>
    Task UpdateAsync(TUpdateModel entityUpdateModel);

    /// <summary>
    /// Delete an entity in the table
    /// </summary>
    /// <param name="id">Primary key of the entity to be deleted</param>
    /// <returns>A task with the action</returns>
    Task DeleteAsync(KeyType id);
}
