using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Generic;

namespace Repository.Repositories.Abstractions;

/// <summary>
/// Repository abstraction for managing <see cref="Character"/> entities.
/// </summary>
public interface ICharacterRepository : IBaseRepository<ulong, Character>
{
    /// <summary>
    /// Retrieves all characters belonging to a specific user.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="behavior">Include behavior for related entities.</param>
    /// <param name="includes">Optional custom includes expression.</param>
    /// <returns>A collection of characters owned by <paramref name="userId"/>.</returns>
    public Task<IEnumerable<Character>> GetByUserIdAsync(
        ulong userId,
        IncludeBehavior behavior,
        Func<IQueryable<Character>, IQueryable<Character>>? includes = null);

    /// <summary>
    /// Retrieves a character by user ID and character name.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="name">The name of the character.</param>
    /// <param name="behavior">Include behavior for related entities.</param>
    /// <param name="includes">Optional custom includes expression.</param>
    /// <returns>The matching <see cref="Character"/>, or <c>null</c> if not found.</returns>
    public Task<Character?> GetByUserAndNameAsync(
        ulong userId,
        string name,
        IncludeBehavior behavior,
        Func<IQueryable<Character>, IQueryable<Character>>? includes = null);

    /// <summary>
    /// Counts the active (non-deleted) characters belonging to a specific user.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns>The total number of characters owned by <paramref name="userId"/>.</returns>
    public Task<int> GetCountByUserIdAsync(ulong userId);

    /// <summary>
    /// Updates <see cref="Character.CurrentGame"/> for all active characters currently associated
    /// with <paramref name="oldGameName"/> (e.g., setting it to <c>null</c> when a game is deleted
    /// or to a new name when a game is renamed).
    /// </summary>
    /// <param name="oldGameName">The previous game name on the characters.</param>
    /// <param name="newGameName">The new game name, or <c>null</c> to clear the game association.</param>
    public Task UpdateCurrentGameAsync(string oldGameName, string? newGameName);
}
