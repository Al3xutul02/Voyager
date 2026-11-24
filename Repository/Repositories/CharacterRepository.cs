using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using Repository.Repositories.Generic;

namespace Repository.Repositories;

/// <summary>
/// Repository implementation for <see cref="Character"/> entities.
/// </summary>
/// <param name="context">The database context to use for queries and persistence.</param>
public class CharacterRepository(DbContext context) : BaseRepository<ulong, Character>(context, "Id"), ICharacterRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Character>> GetByUserIdAsync(
        ulong userId,
        IncludeBehavior behavior,
        Func<IQueryable<Character>, IQueryable<Character>>? includes = null)
    {
        IQueryable<Character> query = new QueryBuilder<Character>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.Where(c => c.UserId == userId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Character?> GetByUserAndNameAsync(
        ulong userId,
        string name,
        IncludeBehavior behavior,
        Func<IQueryable<Character>, IQueryable<Character>>? includes = null)
    {
        IQueryable<Character> query = new QueryBuilder<Character>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(c => c.UserId == userId && c.Name == name);
    }

    /// <inheritdoc />
    public async Task<int> GetCountByUserIdAsync(ulong userId)
    {
        return await _dbSet.CountAsync(c => c.UserId == userId && c.DeletedAt == null);
    }

    /// <inheritdoc />
    public async Task UpdateCurrentGameAsync(string oldGameName, string? newGameName)
    {
        var characters = await _dbSet
            .Where(c => c.CurrentGame == oldGameName && c.DeletedAt == null)
            .ToListAsync();

        foreach (var character in characters)
        {
            character.CurrentGame = newGameName;
            character.UpdatedAt = DateTime.UtcNow;
        }
    }
}
