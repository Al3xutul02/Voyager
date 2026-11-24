using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Generic;

namespace Repository.Repositories.Abstractions;

/// <summary>
/// Repository abstraction for managing <see cref="Game"/> entities and player enrollments.
/// </summary>
public interface IGameRepository : IBaseRepository<ulong, Game>
{
    /// <summary>
    /// Retrieves all games belonging to a specific server.
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <param name="behavior">Include behavior for related entities.</param>
    /// <param name="includes">Optional custom includes expression.</param>
    /// <returns>A collection of games hosted on <paramref name="serverId"/>.</returns>
    public Task<IEnumerable<Game>> GetByServerIdAsync(
        ulong serverId,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null);

    /// <summary>
    /// Retrieves an active game by server ID and game name.
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <param name="name">The name of the game.</param>
    /// <param name="behavior">Include behavior for related entities.</param>
    /// <param name="includes">Optional custom includes expression.</param>
    /// <returns>The matching <see cref="Game"/>, or <c>null</c> if not found.</returns>
    public Task<Game?> GetByServerAndNameAsync(
        ulong serverId,
        string name,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null);

    /// <summary>
    /// Retrieves an active game by server ID and any of its associated Discord channel IDs
    /// (CategoryId, GmChannelId, GeneralChannelId, CharactersChannelId, or NpcsChannelId).
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <param name="channelId">The Discord snowflake ID of the channel or category.</param>
    /// <param name="behavior">Include behavior for related entities.</param>
    /// <param name="includes">Optional custom includes expression.</param>
    /// <returns>The matching <see cref="Game"/>, or <c>null</c> if not found.</returns>
    public Task<Game?> GetByChannelIdAsync(
        ulong serverId,
        ulong channelId,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null);

    /// <summary>
    /// Retrieves all active games where the specified user is either the Game Master or an enrolled player.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <param name="behavior">Include behavior for related entities.</param>
    /// <param name="includes">Optional custom includes expression.</param>
    /// <returns>A collection of active games associated with <paramref name="userId"/>.</returns>
    public Task<IEnumerable<Game>> GetByUserIdAsync(
        ulong userId,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null);

    /// <summary>
    /// Counts the active (non-deleted) games hosted on a specific server.
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <returns>The total number of active games on <paramref name="serverId"/>.</returns>
    public Task<int> GetCountByServerIdAsync(ulong serverId);

    /// <summary>
    /// Checks whether a user is already enrolled as a player in a game.
    /// </summary>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns><c>true</c> if the user is enrolled in the game; otherwise, <c>false</c>.</returns>
    public Task<bool> IsPlayerInGameAsync(ulong gameId, ulong userId);

    /// <summary>
    /// Enrolls a user as a player in a game.
    /// </summary>
    /// <param name="gamePlayer">The join entity to add.</param>
    public Task AddPlayerAsync(GamePlayer gamePlayer);
}
