using BusinessLogic.Dtos.Game;
using BusinessLogic.Services.Generic;
using Repository.Models;

namespace BusinessLogic.Services.Abstractions;

/// <summary>
/// Service abstraction for managing games and player enrollments on servers in Normal mode.
/// </summary>
public interface IGameService
    : IBaseService<ulong, Game, GameReadDto, GameCreateDto, GameUpdateDto>
{
    /// <summary>
    /// Maximum number of active games allowed on a single server.
    /// </summary>
    public const int MaxGamesPerServer = 5;

    /// <summary>
    /// Retrieves all active games hosted on a specific server, including their Game Master and enrolled players.
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <returns>All active games on <paramref name="serverId"/>.</returns>
    public Task<IEnumerable<GameReadDto>> GetByServerIdAsync(ulong serverId);

    /// <summary>
    /// Retrieves an active game by server ID and game name, including its Game Master and enrolled players.
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <param name="name">The name of the game.</param>
    /// <returns>The matching <see cref="GameReadDto"/>, or <c>null</c> if not found or soft-deleted.</returns>
    public Task<GameReadDto?> GetByServerAndNameAsync(ulong serverId, string name);

    /// <summary>
    /// Retrieves an active game by server ID and any of its associated Discord channel IDs
    /// (CategoryId, GmChannelId, GeneralChannelId, CharactersChannelId, or NpcsChannelId).
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <param name="channelId">The Discord snowflake ID of the channel or category.</param>
    /// <returns>The matching <see cref="GameReadDto"/>, or <c>null</c> if not found or soft-deleted.</returns>
    public Task<GameReadDto?> GetByChannelIdAsync(ulong serverId, ulong channelId);

    /// <summary>
    /// Retrieves all active games where the specified user is either the Game Master or an enrolled player.
    /// </summary>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns>All active games associated with <paramref name="userId"/>.</returns>
    public Task<IEnumerable<GameReadDto>> GetByUserIdAsync(ulong userId);

    /// <summary>
    /// Counts the active (non-deleted) games hosted on a specific server.
    /// </summary>
    /// <param name="serverId">The Discord snowflake ID of the server (guild).</param>
    /// <returns>The count of active games on <paramref name="serverId"/>.</returns>
    public Task<int> GetCountByServerIdAsync(ulong serverId);

    /// <summary>
    /// Checks whether a user is already enrolled as a player in a game.
    /// </summary>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    /// <returns><c>true</c> if the user is enrolled in the game; otherwise, <c>false</c>.</returns>
    public Task<bool> IsPlayerInGameAsync(ulong gameId, ulong userId);

    /// <summary>
    /// Enrolls a user as a player in a game if not already enrolled.
    /// </summary>
    /// <param name="gameId">The database ID of the game.</param>
    /// <param name="userId">The Discord snowflake ID of the user.</param>
    public Task AddPlayerAsync(ulong gameId, ulong userId);
}
