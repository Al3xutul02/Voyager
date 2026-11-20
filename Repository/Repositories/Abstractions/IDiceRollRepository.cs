using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Generic;

namespace Repository.Repositories.Abstractions;

/// <summary>
/// Repository abstraction for querying and persisting saved user dice roll
/// (<see cref="DiceRoll"/>) configurations in the database.
/// </summary>
public interface IDiceRollRepository : IBaseRepository<ulong, DiceRoll>
{
    /// <summary>
    /// Retrieves all saved dice roll configurations belonging to a specific user.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="behavior">Specifies how related navigation properties should be included.</param>
    /// <param name="includes">Optional custom include query function when <paramref name="behavior"/> is <see cref="IncludeBehavior.GivenIncludes"/>.</param>
    /// <returns>All <see cref="DiceRoll"/> entities owned by <paramref name="userId"/>.</returns>
    public Task<IEnumerable<DiceRoll>> GetByUserIdAsync(
        ulong userId,
        IncludeBehavior behavior,
        Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>? includes = null);

    /// <summary>
    /// Retrieves a specific saved dice roll configuration by its owning user and name.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="name">The name of the saved dice configuration.</param>
    /// <param name="behavior">Specifies how related navigation properties should be included.</param>
    /// <param name="includes">Optional custom include query function when <paramref name="behavior"/> is <see cref="IncludeBehavior.GivenIncludes"/>.</param>
    /// <returns>The matching <see cref="DiceRoll"/> entity, or <c>null</c> if not found.</returns>
    public Task<DiceRoll?> GetByUserAndNameAsync(
        ulong userId,
        string name,
        IncludeBehavior behavior,
        Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>? includes = null);

    /// <summary>
    /// Retrieves a specific saved dice roll configuration by its owning user and formula.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="formula">The formula of the saved dice configuration.</param>
    /// <param name="behavior">Specifies how related navigation properties should be included.</param>
    /// <param name="includes">Optional custom include query function when <paramref name="behavior"/> is <see cref="IncludeBehavior.GivenIncludes"/>.</param>
    /// <returns>The matching <see cref="DiceRoll"/> entity, or <c>null</c> if not found.</returns>
    public Task<DiceRoll?> GetByUserAndFormulaAsync(
        ulong userId,
        string formula,
        IncludeBehavior behavior,
        Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>? includes = null);
}
