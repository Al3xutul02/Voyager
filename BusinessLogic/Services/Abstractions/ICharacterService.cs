using BusinessLogic.Dtos.Character;
using BusinessLogic.Services.Generic;
using Repository.Models;

namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Service abstraction for managing user characters.
/// </summary>
public interface ICharacterService
    : IBaseService<ulong, Character, CharacterReadDto, CharacterCreateDto, CharacterUpdateDto>
{
    /// <summary>
    /// Maximum number of active characters a single user may have.
    /// </summary>
    public const int MaxCharactersPerUser = 10;

    /// <summary>
    /// Retrieves all active characters belonging to a specific user.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns>All active characters owned by <paramref name="userId"/>.</returns>
    public Task<IEnumerable<CharacterReadDto>> GetByUserIdAsync(ulong userId);

    /// <summary>
    /// Retrieves an active character by its owning user and character name.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="name">The name of the character.</param>
    /// <returns>The matching <see cref="CharacterReadDto"/>, or <c>null</c> if not found or soft-deleted.</returns>
    public Task<CharacterReadDto?> GetByUserAndNameAsync(ulong userId, string name);

    /// <summary>
    /// Counts the active (non-deleted) characters belonging to a specific user.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns>The count of active characters.</returns>
    public Task<int> GetCountByUserIdAsync(ulong userId);
}
