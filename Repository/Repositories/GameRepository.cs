using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using Repository.Repositories.Generic;

namespace Repository.Repositories;

/// <summary>
/// Repository implementation for <see cref="Game"/> entities and <see cref="GamePlayer"/> enrollments.
/// </summary>
/// <param name="context">The database context to use for queries and persistence.</param>
public class GameRepository(DbContext context) : BaseRepository<ulong, Game>(context, "Id"), IGameRepository
{
    private readonly DbSet<GamePlayer> _gamePlayers = context.Set<GamePlayer>();

    /// <inheritdoc />
    public async Task<IEnumerable<Game>> GetByServerIdAsync(
        ulong serverId,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null)
    {
        IQueryable<Game> query = new QueryBuilder<Game>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.Where(g => g.ServerId == serverId && g.DeletedAt == null).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Game?> GetByServerAndNameAsync(
        ulong serverId,
        string name,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null)
    {
        IQueryable<Game> query = new QueryBuilder<Game>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(g => g.ServerId == serverId && g.Name == name && g.DeletedAt == null);
    }

    /// <inheritdoc />
    public async Task<Game?> GetByChannelIdAsync(
        ulong serverId,
        ulong channelId,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null)
    {
        IQueryable<Game> query = new QueryBuilder<Game>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(g =>
            g.ServerId == serverId &&
            g.DeletedAt == null &&
            (g.CategoryId == channelId ||
             g.GmChannelId == channelId ||
             g.GeneralChannelId == channelId ||
             g.CharactersChannelId == channelId ||
             g.NpcsChannelId == channelId));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Game>> GetByUserIdAsync(
        ulong userId,
        IncludeBehavior behavior,
        Func<IQueryable<Game>, IQueryable<Game>>? includes = null)
    {
        IQueryable<Game> query = new QueryBuilder<Game>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query
            .Where(g => g.DeletedAt == null &&
                        (g.GameMasterId == userId || g.Players.Any(p => p.UserId == userId)))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<int> GetCountByServerIdAsync(ulong serverId)
    {
        return await _dbSet.CountAsync(g => g.ServerId == serverId && g.DeletedAt == null);
    }

    /// <inheritdoc />
    public async Task<bool> IsPlayerInGameAsync(ulong gameId, ulong userId)
    {
        return await _gamePlayers.AnyAsync(gp => gp.GameId == gameId && gp.UserId == userId);
    }

    /// <inheritdoc />
    public async Task AddPlayerAsync(GamePlayer gamePlayer)
    {
        await _gamePlayers.AddAsync(gamePlayer);
    }
}
