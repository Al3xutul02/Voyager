using Microsoft.EntityFrameworkCore;
using Repository.Builders;
using Repository.Enums.Behaviors;
using Repository.Models;
using Repository.Repositories.Abstractions;
using Repository.Repositories.Generic;

namespace Repository.Repositories;

/// <summary>
/// Repository implementation for <see cref="DiceRoll"/> entities.
/// </summary>
/// <param name="context">The database context to use for queries and persistence.</param>
public class DiceRollRepository(DbContext context) : BaseRepository<ulong, DiceRoll>(context, "Id"), IDiceRollRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<DiceRoll>> GetByUserIdAsync(
        ulong userId,
        IncludeBehavior behavior,
        Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>? includes = null)
    {
        IQueryable<DiceRoll> query = new QueryBuilder<DiceRoll>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.Where(d => d.UserId == userId).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<DiceRoll?> GetByUserAndNameAsync(
        ulong userId,
        string name,
        IncludeBehavior behavior,
        Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>? includes = null)
    {
        IQueryable<DiceRoll> query = new QueryBuilder<DiceRoll>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(d => d.UserId == userId && d.Name == name);
    }

    /// <inheritdoc />
    public async Task<DiceRoll?> GetByUserAndFormulaAsync(
        ulong userId,
        string formula,
        IncludeBehavior behavior,
        Func<IQueryable<DiceRoll>, IQueryable<DiceRoll>>? includes = null)
    {
        IQueryable<DiceRoll> query = new QueryBuilder<DiceRoll>(_dbSet)
            .AddIncludes(includes)
            .AddBehavior(behavior)
            .Build();

        return await query.FirstOrDefaultAsync(d => d.UserId == userId && d.Formula == formula);
    }
}
