using Repository.Enums.Behaviors;

namespace BusinessLogic.Services.Generic;

/// <summary>
/// General service implementation that contains basic CRUD operations on the database
/// </summary>
/// <typeparam name="T">The class model for the service</typeparam>
public interface IBaseService<T, TReadDto, TCreateDto, TUpdateDto>
    where T : class
    where TReadDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    /// <summary>
    /// Read the entity with a specific key
    /// </summary>
    /// <param name="id">The key of the entity</param>
    /// <returns>A task with the final result of the query</returns>
    Task<TReadDto?> GetByIdAsync(ulong id);

    /// <summary>
    /// Read all entities from the table (WARNING: only use in testing with few entities in a table)
    /// </summary>
    /// <returns>A task with the final result of the query</returns>
    Task<IEnumerable<TReadDto>> GetAllAsync();

    /// <summary>
    /// Create an entity in the table
    /// </summary>
    /// <param name="entityCreateDto">Entity DTO to be used for the creation</param>
    /// <returns>A task with the action</returns>
    Task CreateAsync(TCreateDto entityCreateDto);

    /// <summary>
    /// Update an entity in the table
    /// </summary>
    /// <param name="entityUpdateDto">Entity DTO to be used for the update.
    /// It uses the primary key to find the entity in the table</param>
    /// <returns>A task with the action</returns>
    Task UpdateAsync(TUpdateDto entityUpdateDto);

    /// <summary>
    /// Delete an entity in the table
    /// </summary>
    /// <param name="id">Primary key of the entity to be deleted</param>
    /// <returns>A task with the action</returns>
    Task DeleteAsync(ulong id);
}
